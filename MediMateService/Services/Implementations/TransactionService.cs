using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ==========================================
        // LẤY TẤT CẢ GIAO DỊCH (CHO ADMIN)
        // ==========================================
        public async Task<ApiResponse<PagedResult<TransactionItemDto>>> GetAllTransactionsAsync(TransactionFilterDto filter)
        {
            IQueryable<Transactions> query = _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment)
                    .ThenInclude(p => p != null ? p.User : null)
                .Include(t => t.Payout)
                    .ThenInclude(dp => dp != null ? dp.ConsultationSession : null);

            query = ApplyFiltersAndSorting(query, filter);

            var totalCount = await query.CountAsync();

            var transactions = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var items = transactions.Select(t => new TransactionItemDto
            {
                TransactionId = t.TransactionId,
                TransactionCode = t.TransactionCode,
                TransactionDate = t.PaidAt ?? t.Payment?.CreatedAt ?? DateTime.Now,
                TransactionType = t.TransactionType,
                TotalAmount = t.Payment?.Amount ?? t.Payout?.Amount ?? 0,
                Status = t.TransactionStatus
            }).ToList();

            var result = new PagedResult<TransactionItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<TransactionItemDto>>.Ok(result, "Lấy danh sách giao dịch thành công.");
        }

        // ==========================================
        // [NEW] LẤY GIAO DỊCH THEO USER ID (CHO APP USER/DOCTOR)
        // ==========================================
        public async Task<ApiResponse<PagedResult<TransactionItemDto>>> GetTransactionsByUserIdAsync(Guid userId, TransactionFilterDto filter)
        {
            IQueryable<Transactions> query = _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment)
                    .ThenInclude(p => p != null ? p.User : null)
                .Include(t => t.Payout)
                    .ThenInclude(dp => dp != null ? dp.ConsultationSession : null)
                        .ThenInclude(cs => cs != null ? cs.Doctor : null);

            // Điều kiện lọc theo UserId: 
            // - Nếu là Payment: so sánh với Payment.UserId
            // - Nếu là Payout: so sánh với Payout.ConsultationSession.Doctor.UserId
            query = query.Where(t =>
                (t.Payment != null && t.Payment.UserId == userId) ||
                (t.Payout != null && t.Payout.ConsultationSession != null && t.Payout.ConsultationSession.Doctor != null && t.Payout.ConsultationSession.Doctor.UserId == userId)
            );

            // Dùng chung hàm filter và sort
            query = ApplyFiltersAndSorting(query, filter);

            var totalCount = await query.CountAsync();

            var transactions = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var items = transactions.Select(t => new TransactionItemDto
            {
                TransactionId = t.TransactionId,
                TransactionCode = t.TransactionCode,
                TransactionDate = t.PaidAt ?? t.Payment?.CreatedAt ?? DateTime.Now,
                TransactionType = t.TransactionType,
                TotalAmount = t.Payment?.Amount ?? t.Payout?.Amount ?? 0,
                Status = t.TransactionStatus
            }).ToList();

            var result = new PagedResult<TransactionItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<TransactionItemDto>>.Ok(result, "Lấy danh sách giao dịch của bạn thành công.");
        }

        // ==========================================
        // LẤY CHI TIẾT 1 GIAO DỊCH
        // ==========================================
        public async Task<ApiResponse<TransactionDetailDto>> GetTransactionDetailAsync(Guid transactionId)
        {
            var transaction = await _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment)
                    .ThenInclude(p => p!.User)
                .Include(t => t.Payment)
                    .ThenInclude(p => p!.Subscription)
                .Include(t => t.Payout)
                    .ThenInclude(dp => dp!.ConsultationSession)
                        .ThenInclude(cs => cs!.Doctor)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null)
            {
                return ApiResponse<TransactionDetailDto>.Fail("Không tìm thấy giao dịch.", 404);
            }

            var detail = new TransactionDetailDto
            {
                TransactionId = transaction.TransactionId,
                SenderName = transaction.TransactionType == TransactionTypes.MoneyReceived
                    ? (transaction.Payment?.User?.FullName ?? "Unknown")
                    : "MediMate",
                ReceiverName = transaction.TransactionType == TransactionTypes.MoneyReceived
                    ? "MediMate"
                    : $"Bác sĩ {transaction.Payout?.ConsultationSession?.Doctor?.FullName ?? "Unknown"}",
                TransactionType = transaction.TransactionType,
                Content = transaction.Payment?.PaymentContent ?? "",
                Amount = transaction.Payment?.Amount ?? transaction.Payout?.Amount ?? 0,
                TransactionCode = transaction.TransactionCode,
                PaymentCode = transaction.GatewayTransactionId ?? "",
                AppointmentDate = transaction.PaidAt,
                PaymentMethod = transaction.GatewayName ?? "Chuyển khoản",
                PaymentStatus = transaction.TransactionStatus
            };

            return ApiResponse<TransactionDetailDto>.Ok(detail, "Lấy chi tiết giao dịch thành công.");
        }

        public async Task<ApiResponse<bool>> UpdateTransactionStatusAsync(Guid transactionId, string status)
        {
            try
            {
                // 1. Chuẩn hóa trạng thái (VD: "failed", "FAILED", "Failed" -> "Failed")
                var formattedStatus = char.ToUpper(status.Trim()[0]) + status.Trim().Substring(1).ToLower();
                var allowedStatuses = new[] { "Success", "Failed", "Cancelled", "Pending" };

                if (!allowedStatuses.Contains(formattedStatus))
                {
                    return ApiResponse<bool>.Fail($"Trạng thái '{status}' không hợp lệ. Chỉ chấp nhận: Success, Failed, Cancelled, Pending.", 400);
                }

                // 2. Tìm giao dịch kèm theo Payment và Subscription
                var transaction = await _unitOfWork.Repository<Transactions>().GetQueryable()
                    .Include(t => t.Payment)
                        .ThenInclude(p => p!.Subscription)
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

                if (transaction == null)
                {
                    return ApiResponse<bool>.Fail("Không tìm thấy giao dịch.", 404);
                }

                // 3. Chặn không cho đổi trạng thái nếu đã thành công
                if (transaction.TransactionStatus == "Success")
                {
                    return ApiResponse<bool>.Fail("Giao dịch này đã thành công trước đó, không thể thay đổi trạng thái.", 400);
                }

                // 4. Cập nhật Transaction
                transaction.TransactionStatus = formattedStatus;

                // Nếu là Failed hoặc Cancelled thì ghi nhận thời điểm hủy
                if (formattedStatus == "Failed" || formattedStatus == "Cancelled")
                {
                    transaction.PaidAt = DateTime.Now;
                }

                // 5. Đồng bộ trạng thái sang bảng Payment và Subscription (nếu có)
                if (transaction.Payment != null)
                {
                    transaction.Payment.Status = formattedStatus;

                    // Chỉ hủy gói Subscription nếu gói này chưa bị xử lý
                    if (transaction.Payment.Subscription != null &&
                        transaction.Payment.Subscription.Status != "Active" &&
                        transaction.Payment.Subscription.Status != "Inactive")
                    {
                        transaction.Payment.Subscription.Status = formattedStatus;
                    }
                }

                // Lưu thay đổi
                _unitOfWork.Repository<Transactions>().Update(transaction);
                await _unitOfWork.CompleteAsync();

                return ApiResponse<bool>.Ok(true, $"Đã cập nhật trạng thái giao dịch thành {formattedStatus}.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }

        // ==========================================
        // HELPER: HÀM DÙNG CHUNG ĐỂ LỌC VÀ SẮP XẾP
        // ==========================================
        private IQueryable<Transactions> ApplyFiltersAndSorting(IQueryable<Transactions> query, TransactionFilterDto filter)
        {
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(t =>
                    t.TransactionCode.ToLower().Contains(term) ||
                    (t.GatewayTransactionId != null && t.GatewayTransactionId.ToLower().Contains(term))
                );
            }

            if (!string.IsNullOrEmpty(filter.Type))
            {
                var type = filter.Type.ToLower();
                query = query.Where(t => t.TransactionType.ToLower() == type);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                var status = filter.Status.ToLower();
                query = query.Where(t => t.TransactionStatus.ToLower() == status);
            }

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var sortBy = filter.SortBy.ToLower();
                switch (sortBy)
                {
                    case "transactiondate":
                        query = filter.IsDescending
                            ? query.OrderByDescending(t => t.PaidAt ?? t.Payment.CreatedAt)
                            : query.OrderBy(t => t.PaidAt ?? t.Payment.CreatedAt);
                        break;
                    default:
                        query = filter.IsDescending
                            ? query.OrderByDescending(t => t.PaidAt ?? t.Payment.CreatedAt)
                            : query.OrderBy(t => t.PaidAt ?? t.Payment.CreatedAt);
                        break;
                }
            }
            else
            {
                // Mặc định sắp xếp mới nhất lên đầu
                query = query.OrderByDescending(t => t.PaidAt ?? DateTime.MinValue);
            }

            return query;
        }
    public async Task<ApiResponse<TransactionDetailDto>> GetTransactionByPaymentIdAsync(Guid paymentId)
        {
            var transaction = await _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment)
                    .ThenInclude(p => p!.User)
                .Include(t => t.Payment)
                    .ThenInclude(p => p!.Subscription)
                .FirstOrDefaultAsync(t => t.PaymentId == paymentId);

            if (transaction == null)
            {
                return ApiResponse<TransactionDetailDto>.Fail("Không tìm thấy giao dịch cho thanh toán này.", 404);
            }

            var detail = new TransactionDetailDto
            {
                TransactionId = transaction.TransactionId,
                SenderName = transaction.Payment?.User?.FullName ?? "Unknown",
                ReceiverName = "MediMate",
                TransactionType = transaction.TransactionType,
                Content = transaction.Payment?.PaymentContent ?? "",
                Amount = transaction.Payment?.Amount ?? 0,
                TransactionCode = transaction.TransactionCode,
                PaymentCode = transaction.GatewayTransactionId ?? "",
                AppointmentDate = transaction.PaidAt,
                PaymentMethod = transaction.GatewayName ?? "Chuyển khoản",
                PaymentStatus = transaction.TransactionStatus
            };

            return ApiResponse<TransactionDetailDto>.Ok(detail, "Lấy chi tiết giao dịch thành công.");
        }
    }
}