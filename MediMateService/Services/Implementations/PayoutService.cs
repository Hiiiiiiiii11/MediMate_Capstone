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
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly IEmailService _emailService;

        public PayoutService(IUnitOfWork unitOfWork, IUploadPhotoService uploadPhotoService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _uploadPhotoService = uploadPhotoService;
            _emailService = emailService;
        }

        // ─────────────────────────────────────────────────────────────────
        // LẤY DANH SÁCH PAYOUT (có phân trang & filter)
        // ─────────────────────────────────────────────────────────────────
        public async Task<PagedResult<PayoutItemDto>> GetPayoutsAsync(PayoutFilterDto filter)
        {
            var query = _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.Clinic)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Member)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Payments)
                        .ThenInclude(pay => pay.User)
                .Include(p => p.ConsultationSession)
                    .ThenInclude(c => c.Member)
                .Include(p => p.ConsultationSession)
                    .ThenInclude(c => c.Doctor)
                .Include(p => p.ConsultationSession)
                    .ThenInclude(c => c.Appointment)
                        .ThenInclude(a => a.Payments)
                            .ThenInclude(pay => pay.User)
                .AsNoTracking();

            if (filter.ClinicId.HasValue)
                query = query.Where(p => p.ClinicId == filter.ClinicId.Value);

            if (filter.DoctorId.HasValue)
                query = query.Where(p =>
                    (p.Appointment != null && p.Appointment.DoctorId == filter.DoctorId.Value) ||
                    (p.ConsultationSession != null && p.ConsultationSession.DoctorId == filter.DoctorId.Value));

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(p => p.Status == filter.Status);

            var totalCount = await query.CountAsync();


            var items = await query
                .OrderByDescending(p => p.CalculatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Lấy tất cả UserId của người thanh toán để tra bank account một lần (tránh N+1)
            var payerUserIds = items
                .Select(p =>
                {
                    var appt = p.Appointment ?? p.ConsultationSession?.Appointment;
                    return appt?.Payments?.FirstOrDefault()?.UserId;
                })
                .Where(id => id != null)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var bankAccounts = payerUserIds.Any()
                ? await _unitOfWork.Repository<UserBankAccount>()
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(b => payerUserIds.Contains(b.UserId))
                    .ToDictionaryAsync(b => b.UserId, b => b)
                : new Dictionary<Guid, UserBankAccount>();

            return new PagedResult<PayoutItemDto>
            {
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                Items = items.Select(p => MapPayoutItemDto(p, bankAccounts)).ToList()
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
            // Lấy Clinic riêng để tránh EF Core track duplicate qua Include
            var clinic = await _unitOfWork.Repository<Clinics>()
                .GetQueryable()
                .Include(c => c.Admin)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClinicId == clinicId);

            // Lấy các payout đang chờ, KHÔNG include Clinic để tránh conflict tracking
            var pendingPayouts = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Where(p => p.ClinicId == clinicId && p.Status == "ReadyToPay")
                .ToListAsync();

            if (!pendingPayouts.Any())
                throw new NotFoundException($"Không có khoản công nợ nào ở trạng thái ReadyToPay cho phòng khám này.");

            string? transferImageUrl = null;
            if (dto.TransferImage != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(dto.TransferImage);
                transferImageUrl = uploadResult.OriginalUrl;
            }

            string? reportFileUrl = null;
            if (dto.ReportFile != null)
            {
                reportFileUrl = await _uploadPhotoService.UploadDocumentAsync(dto.ReportFile);
            }

            var now = DateTime.Now;
            decimal totalAmount = 0;

            foreach (var payout in pendingPayouts)
            {
                payout.Status = "Paid";
                payout.PaidAt = now;
                payout.TransferImageUrl = transferImageUrl;
                payout.ReportFileUrl = reportFileUrl;
                totalAmount += payout.Amount;
                _unitOfWork.Repository<DoctorPayout>().Update(payout);
            }

            await _unitOfWork.CompleteAsync();

            // Gửi Email thông báo cho Clinic
            var targetEmail = clinic?.Email ?? clinic?.Admin?.Email;
            if (!string.IsNullOrEmpty(targetEmail))
            {
                var subject = $"[MediMate] Thông báo Tất toán Công nợ - {now:dd/MM/yyyy}";
                var emailBody = $@"
                    <h3>Kính gửi phòng khám {clinic.Name},</h3>
                    <p>MediMate thông báo đã thực hiện tất toán số tiền <strong>{totalAmount:N0} VNĐ</strong> cho các lượt khám thành công.</p>
                    <p><strong>Ngày tất toán:</strong> {now:dd/MM/yyyy HH:mm}</p>
                    <ul>
                        {(transferImageUrl != null ? $"<li><a href='{transferImageUrl}'>Xem Hình ảnh Ủy nhiệm chi (Chuyển khoản)</a></li>" : "")}
                        {(reportFileUrl != null ? $"<li><a href='{reportFileUrl}'>Tải Báo cáo Danh sách Bác sĩ / Lượt khám đính kèm</a></li>" : "")}
                    </ul>
                    <p>Ghi chú từ Admin: {dto.Note}</p>
                    <p>Trân trọng,<br>Đội ngũ MediMate</p>
                ";

                // Await trực tiếp để đảm bảo service không bị dispose trước khi chạy xong
                await _emailService.SendEmailAsync(targetEmail, subject, emailBody);
            }

            return pendingPayouts.Count;
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────────────
        private static PayoutItemDto MapPayoutItemDto(DoctorPayout p, Dictionary<Guid, UserBankAccount> bankAccounts)
        {
            var appointment = p.Appointment ?? p.ConsultationSession?.Appointment;
            var payment = appointment?.Payments?.FirstOrDefault();
            var payerUserId = payment?.UserId;
            bankAccounts.TryGetValue(payerUserId ?? Guid.Empty, out var bankAccount);

            return new PayoutItemDto
            {
                PayoutId = p.PayoutId,
                ClinicId = p.ClinicId,
                ClinicName = p.Clinic?.Name ?? string.Empty,
                AppointmentId = p.AppointmentId,
                AppointmentDate = p.Appointment?.AppointmentDate,
                AppointmentTime = p.Appointment?.AppointmentTime,
                PatientName = p.Appointment?.Member?.FullName ?? p.ConsultationSession?.Member?.FullName,
                DoctorName = p.Appointment?.Doctor?.FullName ?? p.ConsultationSession?.Doctor?.FullName,

                PaymentStatus = appointment?.PaymentStatus,
                PayerName = payment?.User?.FullName,
                PayerPhoneNumber = payment?.User?.PhoneNumber,
                PayerBankName = bankAccount?.BankName,
                PayerBankAccountNumber = bankAccount?.AccountNumber,
                PayerBankAccountHolder = bankAccount?.AccountHolder,

                ConsultationId = p.ConsultationId,
                Amount = p.Amount,
                Status = p.Status,
                CalculatedAt = p.CalculatedAt,
                PaidAt = p.PaidAt,
                TransferImageUrl = p.TransferImageUrl,
                ReportFileUrl = p.ReportFileUrl
            };
        }
    }
}
