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
        private readonly INotificationService _notificationService;
        public TransactionService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
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
                .Include(t => t.Payout)
                    .ThenInclude(dp => dp!.ConsultationSession)
                        .ThenInclude(cs => cs!.Doctor)
                .FirstOrDefaultAsync(t => t.PaymentId == paymentId);

            if (transaction == null)
            {
                return ApiResponse<TransactionDetailDto>.Fail("Không tìm thấy giao dịch cho thanh toán này.", 404);
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

        public async Task<ApiResponse<PagedResult<PendingPayoutDto>>> GetPendingPayoutsAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            var query = _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.ConsultationSession)
                    .ThenInclude(cs => cs!.Doctor)
                        .ThenInclude(d => d!.User) // Include để lấy tên
                .Include(p => p.ConsultationSession)
                    .ThenInclude(cs => cs!.Doctor)
                        .ThenInclude(d => d!.DoctorBankAccount) // Include để lấy STK ngân hàng
                .Where(p => p.Status == "Pending"); // Chỉ lấy các phiếu đang nợ

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p => p.ConsultationSession.Doctor.FullName.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            var payouts = await query
                .OrderBy(p => p.CalculatedAt) // Ưu tiên trả nợ cũ trước
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = payouts.Select(p => new PendingPayoutDto
            {
                PayoutId = p.PayoutId,
                DoctorId = p.ConsultationSession.DoctorId,
                DoctorName = p.ConsultationSession.Doctor?.User?.FullName ?? p.ConsultationSession.Doctor?.FullName ?? "Unknown",
                BankName = p.ConsultationSession.Doctor?.DoctorBankAccount?.BankName ?? "Chưa cập nhật",
                AccountNumber = p.ConsultationSession.Doctor?.DoctorBankAccount?.AccountNumber ?? "Chưa cập nhật",
                AccountHolder = p.ConsultationSession.Doctor?.DoctorBankAccount?.AccountHolder ?? "Chưa cập nhật",
                Amount = p.Amount,
                CalculatedAt = p.CalculatedAt,
                Status = p.Status
            }).ToList();

            var result = new PagedResult<PendingPayoutDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResult<PendingPayoutDto>>.Ok(result, "Lấy danh sách cần giải ngân thành công.");
        }

        public async Task<ApiResponse<PagedResult<PaidPayoutDto>>> GetPaidPayoutsAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            var query = _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.ConsultationSession)
                    .ThenInclude(cs => cs!.Doctor)
                        .ThenInclude(d => d!.User)
                .Include(p => p.ConsultationSession)
                    .ThenInclude(cs => cs!.Doctor)
                        .ThenInclude(d => d!.DoctorBankAccount)
                .Where(p => p.Status == "Paid")
                .OrderByDescending(p => p.PaidAt);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = (IOrderedQueryable<DoctorPayout>)query.Where(p =>
                    p.ConsultationSession.Doctor.FullName.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            var payouts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy BankTransactionCode (GatewayTransactionId) từ bảng Transactions
            var payoutIds = payouts.Select(p => p.PayoutId).ToList();
            var txCodes = await _unitOfWork.Repository<Transactions>().GetQueryable()
                .Where(t => t.PayoutId != null && payoutIds.Contains(t.PayoutId.Value))
                .Select(t => new { t.PayoutId, t.GatewayTransactionId })
                .ToListAsync();

            var items = payouts.Select(p =>
            {
                var bankTxCode = txCodes.FirstOrDefault(t => t.PayoutId == p.PayoutId)?.GatewayTransactionId;

                return new PaidPayoutDto
                {
                    PayoutId = p.PayoutId,
                    DoctorId = p.ConsultationSession.DoctorId,
                    DoctorName = p.ConsultationSession.Doctor?.User?.FullName ?? p.ConsultationSession.Doctor?.FullName ?? "Unknown",
                    BankName = p.ConsultationSession.Doctor?.DoctorBankAccount?.BankName ?? "Chưa cập nhật",
                    AccountNumber = p.ConsultationSession.Doctor?.DoctorBankAccount?.AccountNumber ?? "Chưa cập nhật",
                    AccountHolder = p.ConsultationSession.Doctor?.DoctorBankAccount?.AccountHolder ?? "Chưa cập nhật",
                    Amount = p.Amount,
                    CalculatedAt = p.CalculatedAt,
                    PaidAt = p.PaidAt,
                    TransferImageUrl = p.TransferImageUrl,
                    BankTransactionCode = bankTxCode
                };
            }).ToList();

            var result = new PagedResult<PaidPayoutDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResult<PaidPayoutDto>>.Ok(result, "Lấy lịch sử giải ngân thành công.");
        }

        public async Task<ApiResponse<bool>> ApproveDoctorPayoutAsync(Guid payoutId, ApprovePayoutRequest request, string? transferImageUrl = null)
        {
            var payout = await _unitOfWork.Repository<DoctorPayout>().GetQueryable()
                .Include(p => p.ConsultationSession)
                    .ThenInclude(cs => cs!.Doctor)
                .FirstOrDefaultAsync(p => p.PayoutId == payoutId);

            if (payout == null)
                return ApiResponse<bool>.Fail("Không tìm thấy phiếu giải ngân này.", 404);

            if (payout.Status == "Paid" || payout.Status == "Success")
                return ApiResponse<bool>.Fail("Phiếu này đã được thanh toán rồi.", 400);

            if (string.IsNullOrWhiteSpace(request.BankTransactionCode))
                return ApiResponse<bool>.Fail("Vui lòng nhập Mã giao dịch ngân hàng để làm bằng chứng.", 400);

            // 1. Cập nhật Payout thành Đã Trả (Paid)
            payout.Status = "Paid";
            payout.PaidAt = DateTime.Now;
            payout.TransferImageUrl = transferImageUrl;
            _unitOfWork.Repository<DoctorPayout>().Update(payout);

            // 2. Ghi sổ Transaction (Dòng tiền đi ra)
            var transaction = new Transactions
            {
                TransactionId = Guid.NewGuid(),
                TransactionCode = $"PAYOUT-{DateTime.Now.Ticks % 100000}",
                PayoutId = payout.PayoutId,
                GatewayName = "BankTransfer", 
                GatewayTransactionId = request.BankTransactionCode, 
                TransactionStatus = "Success",
                TransactionType = TransactionTypes.MoneySent, // Quan trọng
                AmountPaid = payout.Amount,
                PaidAt = DateTime.Now
            };

            await _unitOfWork.Repository<Transactions>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();

            // 3. [NEW] Bắn Notification báo tiền về cho Bác sĩ
            if (payout.ConsultationSession?.Doctor != null)
            {
                await _notificationService.SendNotificationAsync(
                    userId: payout.ConsultationSession.Doctor.UserId,
                    title: "💰 Nhận tiền tư vấn",
                    message: $"MediMate đã thanh toán {payout.Amount:N0}đ cho ca tư vấn của bạn. Mã GD: {request.BankTransactionCode}. Vui lòng kiểm tra tài khoản.",
                    type: "PAYOUT_SUCCESS", // Type tuỳ m định nghĩa ở Constants
                    referenceId: payout.PayoutId
                );
            }

            return ApiResponse<bool>.Ok(true, "Đã ghi nhận thanh toán cho bác sĩ thành công!");
        }
    }
}