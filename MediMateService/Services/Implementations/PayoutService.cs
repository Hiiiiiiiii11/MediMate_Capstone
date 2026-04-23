using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Microsoft.EntityFrameworkCore;

namespace MediMateService.Services.Implementations
{
    public class PayoutService : IPayoutService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PayoutService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ─────────────────────────────────────────────────────────────────
        // LẤY DANH SÁCH PAYOUT (có phân trang & filter)
        // ─────────────────────────────────────────────────────────────────
        public async Task<PagedResult<PayoutItemDto>> GetPayoutsAsync(PayoutFilterDto filter)
        {
            var query = _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.Clinic)
                .AsNoTracking();

            if (filter.ClinicId.HasValue)
                query = query.Where(p => p.ClinicId == filter.ClinicId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(p => p.Status == filter.Status);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CalculatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<PayoutItemDto>
            {
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                Items = items.Select(MapPayoutItemDto).ToList()
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // TỔNG HỢP CÔNG NỢ THEO PHÒNG KHÁM
        // ─────────────────────────────────────────────────────────────────
        public async Task<IReadOnlyList<PayoutSummaryDto>> GetPayoutSummaryByClinicAsync()
        {
            var payouts = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.Clinic)
                .AsNoTracking()
                .Where(p => p.ClinicId != null)
                .ToListAsync();

            var summaries = payouts
                .GroupBy(p => new { p.ClinicId, ClinicName = p.Clinic?.Name ?? "Không rõ" })
                .Select(g => new PayoutSummaryDto
                {
                    ClinicId = g.Key.ClinicId!.Value,
                    ClinicName = g.Key.ClinicName,
                    TotalPendingAmount = g.Where(p => p.Status == "ReadyToPay").Sum(p => p.Amount),
                    PendingPayoutCount = g.Count(p => p.Status == "ReadyToPay"),
                    TotalPaidAmount = g.Where(p => p.Status == "Paid").Sum(p => p.Amount)
                })
                .OrderByDescending(s => s.TotalPendingAmount)
                .ToList();

            return summaries;
        }

        // ─────────────────────────────────────────────────────────────────
        // ADMIN XÁC NHẬN THANH TOÁN CHO PHÒNG KHÁM (batch update)
        // ─────────────────────────────────────────────────────────────────
        public async Task<int> ProcessClinicPayoutAsync(Guid clinicId, ProcessPayoutDto dto)
        {
            var pendingPayouts = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Where(p => p.ClinicId == clinicId && p.Status == "ReadyToPay")
                .ToListAsync();

            if (!pendingPayouts.Any())
                throw new NotFoundException($"Không có khoản công nợ nào ở trạng thái ReadyToPay cho phòng khám này.");

            var now = DateTime.Now;
            foreach (var payout in pendingPayouts)
            {
                payout.Status = "Paid";
                payout.PaidAt = now;
                payout.TransferImageUrl = dto.TransferImageUrl;
                _unitOfWork.Repository<DoctorPayout>().Update(payout);
            }

            await _unitOfWork.CompleteAsync();
            return pendingPayouts.Count;
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────────────
        private static PayoutItemDto MapPayoutItemDto(DoctorPayout p) => new()
        {
            PayoutId = p.PayoutId,
            ClinicId = p.ClinicId,
            ClinicName = p.Clinic?.Name ?? string.Empty,
            AppointmentId = p.AppointmentId,
            ConsultationId = p.ConsultationId,
            Amount = p.Amount,
            Status = p.Status,
            CalculatedAt = p.CalculatedAt,
            PaidAt = p.PaidAt,
            TransferImageUrl = p.TransferImageUrl
        };
    }
}
