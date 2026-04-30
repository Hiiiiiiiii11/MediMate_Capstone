using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using Microsoft.EntityFrameworkCore;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public TransactionService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        #region Transaction Queries (Admin & User)

        public async Task<ApiResponse<PagedResult<TransactionItemDto>>> GetAllTransactionsAsync(TransactionFilterDto filter)
        {
            var query = GetFullTransactionQuery();
            query = ApplyFiltersAndSorting(query, filter);

            var totalCount = await query.CountAsync();
            var transactions = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResult<TransactionItemDto>>.Ok(new PagedResult<TransactionItemDto>
            {
                Items = transactions.Select(MapToItemDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            }, "Lấy danh sách giao dịch thành công.");
        }

        public async Task<ApiResponse<PagedResult<TransactionItemDto>>> GetTransactionsByUserIdAsync(Guid userId, TransactionFilterDto filter)
        {
            var doctor = await _unitOfWork.Repository<Doctors>().GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);

            Guid? doctorId = doctor?.DoctorId;

            var query = GetFullTransactionQuery();
            query = query.Where(t =>
                (t.Payment != null && t.Payment.UserId == userId) ||
                (doctorId != null && t.Payout != null && t.Payout.ConsultationSession != null && t.Payout.ConsultationSession.DoctorId == doctorId.Value)
            );

            query = ApplyFiltersAndSorting(query, filter);
            var totalCount = await query.CountAsync();
            var transactions = await query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();

            return ApiResponse<PagedResult<TransactionItemDto>>.Ok(new PagedResult<TransactionItemDto>
            {
                Items = transactions.Select(MapToItemDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<TransactionDetailDto>> GetTransactionDetailAsync(Guid transactionId)
        {
            var t = await GetFullTransactionQuery().FirstOrDefaultAsync(t => t.TransactionId == transactionId);
            if (t == null) return ApiResponse<TransactionDetailDto>.Fail("Không tìm thấy giao dịch.");
            return ApiResponse<TransactionDetailDto>.Ok(MapToDetailDto(t));
        }

        public async Task<ApiResponse<TransactionDetailDto>> GetTransactionByPaymentIdAsync(Guid paymentId)
        {
            var t = await GetFullTransactionQuery().FirstOrDefaultAsync(t => t.PaymentId == paymentId);
            if (t == null) return ApiResponse<TransactionDetailDto>.Fail("Không tìm thấy giao dịch.");
            return ApiResponse<TransactionDetailDto>.Ok(MapToDetailDto(t));
        }

        #endregion

        #region Transaction & Payout Management

        public async Task<ApiResponse<bool>> UpdateTransactionStatusAsync(Guid transactionId, string status)
        {
            var formattedStatus = char.ToUpper(status.Trim()[0]) + status.Trim().Substring(1).ToLower();
            var transaction = await _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment).ThenInclude(p => p!.Subscription)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null) return ApiResponse<bool>.Fail("Không tìm thấy giao dịch.");
            if (transaction.TransactionStatus == "Success") return ApiResponse<bool>.Fail("Giao dịch đã thành công, không thể sửa.");

            transaction.TransactionStatus = formattedStatus;
            if (formattedStatus == "Failed" || formattedStatus == "Cancelled") transaction.PaidAt = DateTime.Now;

            if (transaction.Payment != null)
            {
                transaction.Payment.Status = formattedStatus;
                if (transaction.Payment.Subscription != null && transaction.Payment.Subscription.Status == "Pending")
                    transaction.Payment.Subscription.Status = formattedStatus;
            }

            _unitOfWork.Repository<Transactions>().Update(transaction);
            await _unitOfWork.CompleteAsync();
            return ApiResponse<bool>.Ok(true, "Cập nhật trạng thái thành công.");
        }

        public async Task<ApiResponse<PagedResult<PendingPayoutDto>>> GetPendingPayoutsAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            var query = _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.ConsultationSession).ThenInclude(cs => cs!.Doctor).ThenInclude(d => d!.User)
                .Include(p => p.ConsultationSession).ThenInclude(cs => cs!.Doctor).ThenInclude(d => d!.DoctorBankAccount)
                .Where(p => p.Status == "Pending");

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(p => p.ConsultationSession.Doctor.FullName.ToLower().Contains(searchTerm.ToLower()));

            var totalCount = await query.CountAsync();
            var payouts = await query.OrderBy(p => p.CalculatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return ApiResponse<PagedResult<PendingPayoutDto>>.Ok(new PagedResult<PendingPayoutDto>
            {
                Items = payouts.Select(p => new PendingPayoutDto
                {
                    PayoutId = p.PayoutId,
                    DoctorId = p.ConsultationSession.DoctorId,
                    DoctorName = p.ConsultationSession.Doctor?.FullName ?? "N/A",
                    BankName = p.ConsultationSession.Doctor?.DoctorBankAccount?.BankName ?? "N/A",
                    AccountNumber = p.ConsultationSession.Doctor?.DoctorBankAccount?.AccountNumber ?? "N/A",
                    AccountHolder = p.ConsultationSession.Doctor?.DoctorBankAccount?.AccountHolder ?? "N/A",
                    Amount = p.Amount,
                    CalculatedAt = p.CalculatedAt,
                    Status = p.Status
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        public async Task<ApiResponse<PagedResult<PaidPayoutDto>>> GetPaidPayoutsAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            var query = _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.ConsultationSession).ThenInclude(cs => cs!.Doctor).ThenInclude(d => d!.User)
                .Where(p => p.Status == "Paid");

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(p => p.ConsultationSession.Doctor.FullName.ToLower().Contains(searchTerm.ToLower()));

            var totalCount = await query.CountAsync();
            var payouts = await query.OrderByDescending(p => p.PaidAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return ApiResponse<PagedResult<PaidPayoutDto>>.Ok(new PagedResult<PaidPayoutDto>
            {
                Items = payouts.Select(p => new PaidPayoutDto
                {
                    PayoutId = p.PayoutId,
                    DoctorName = p.ConsultationSession.Doctor?.FullName ?? "N/A",
                    Amount = p.Amount,
                    PaidAt = p.PaidAt,
                    TransferImageUrl = p.TransferImageUrl
                }).ToList(),
                TotalCount = totalCount
            });
        }

        public async Task<ApiResponse<bool>> ApproveDoctorPayoutAsync(Guid payoutId, ApprovePayoutRequest request, string? transferImageUrl = null)
        {
            var payout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.ConsultationSession).ThenInclude(cs => cs!.Doctor)
                .FirstOrDefaultAsync(p => p.PayoutId == payoutId);

            if (payout == null) return ApiResponse<bool>.Fail("Không tìm thấy phiếu.");

            payout.Status = "Paid";
            payout.PaidAt = DateTime.Now;
            payout.TransferImageUrl = transferImageUrl;
            _unitOfWork.Repository<DoctorPayout>().Update(payout);

            var transaction = new Transactions
            {
                TransactionCode = $"PAYOUT-{DateTime.Now.Ticks % 100000}",
                PayoutId = payout.PayoutId,
                GatewayName = "BankTransfer",
                GatewayTransactionId = request.BankTransactionCode,
                TransactionStatus = "Success",
                TransactionType = TransactionTypes.OutClinicPayout,
                AmountPaid = payout.Amount,
                GatewayResponse = transferImageUrl,
                PaidAt = DateTime.Now
            };

            await _unitOfWork.Repository<Transactions>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();
            return ApiResponse<bool>.Ok(true, "Giải ngân thành công.");
        }

        #endregion

        #region Helpers

        private IQueryable<Transactions> GetFullTransactionQuery()
        {
            return _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment).ThenInclude(p => p!.User)
                .Include(t => t.Payout).ThenInclude(dp => dp!.ConsultationSession).ThenInclude(cs => cs!.Doctor)
                .AsNoTracking();
        }

        private TransactionItemDto MapToItemDto(Transactions t)
        {
            return new TransactionItemDto
            {
                TransactionId = t.TransactionId,
                TransactionCode = t.TransactionCode,
                TransactionDate = t.PaidAt ?? t.Payment?.CreatedAt ?? DateTime.Now,
                TransactionType = t.TransactionType,
                Content = t.Payment != null ? (t.Payment.PaymentContent ?? "Giao dịch") : $"Giải ngân tư vấn #{t.Payout?.ConsultationId}",
                TotalAmount = t.AmountPaid,
                Status = t.TransactionStatus,
                GatewayResponse = t.GatewayResponse
            };
        }

        private TransactionDetailDto MapToDetailDto(Transactions t)
        {
            return new TransactionDetailDto
            {
                TransactionId = t.TransactionId,
                TransactionType = t.TransactionType,
                Content = t.Payment != null ? (t.Payment.PaymentContent ?? "Giao dịch") : (t.Payout != null ? "Thanh toán thù lao" : "Giao dịch"),
                Amount = t.AmountPaid,
                GatewayResponse = t.GatewayResponse,
                PaymentStatus = t.TransactionStatus,
                TransactionCode = t.TransactionCode,
                PaymentCode = t.GatewayTransactionId
            };
        }

        private IQueryable<Transactions> ApplyFiltersAndSorting(IQueryable<Transactions> query, TransactionFilterDto filter)
        {
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(t => t.TransactionCode.Contains(filter.SearchTerm));
            return filter.IsDescending ? query.OrderByDescending(t => t.PaidAt) : query.OrderBy(t => t.PaidAt);
        }

        #endregion
    }
}