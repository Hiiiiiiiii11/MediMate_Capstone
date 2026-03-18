using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MediMateService.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<PagedResult<TransactionItemDto>>> GetAllTransactionsAsync(TransactionFilterDto filter)
        {
            IQueryable<Transactions> query = _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment)
                    .ThenInclude(p => p != null ? p.User : null)
                .Include(t => t.Payout)
                    .ThenInclude(dp => dp != null ? dp.ConsultationSession : null);

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

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var sortBy = filter.SortBy.ToLower();
                switch (sortBy)
                {
                    case "transactiondate":
                        query = filter.IsDescending 
                            ? query.OrderByDescending(t => t.Payment.CreatedAt) 
                            : query.OrderBy(t => t.Payment.CreatedAt);
                        break;
                    case "totalamount":
                        query = filter.IsDescending 
                            ? query.OrderByDescending(t => t.AmountPaid) 
                            : query.OrderBy(t => t.AmountPaid);
                        break;
                    default:
                        query = filter.IsDescending 
                            ? query.OrderByDescending(t => t.Payment.CreatedAt) 
                            : query.OrderBy(t => t.Payment.CreatedAt);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(t => t.PaidAt ?? DateTime.MinValue);
            }

            var transactions = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var items = transactions.Select(t => new TransactionItemDto
            {
                TransactionId = t.TransactionId,
                TransactionCode = t.TransactionCode,
                TransactionDate = t.PaidAt ?? t.Payment?.CreatedAt ?? DateTime.UtcNow,
                TransactionType = t.TransactionType,
                TotalAmount = t.AmountPaid,
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

        public async Task<ApiResponse<TransactionDetailDto>> GetTransactionDetailAsync(Guid transactionId)
        {
            var transaction = await _unitOfWork.Repository<Transactions>().GetQueryable()
                .Include(t => t.Payment)
                    .ThenInclude(p => p!.User)
                .Include(t => t.Payment)
                    .ThenInclude(p => p!.Subscription)
                .Include(t => t.Payout)
                    .ThenInclude(dp => dp!.ConsultationSession)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null)
            {
                return ApiResponse<TransactionDetailDto>.Fail("Không tìm thấy giao dịch.", 404);
            }

            var appointmentDate = transaction.Payment?.Subscription?.StartDate.ToDateTime(TimeOnly.MinValue);

            var detail = new TransactionDetailDto
            {
                TransactionId = transaction.TransactionId,
                SenderName = transaction.TransactionType == "Tiền nhận vào"
                    ? (transaction.Payment?.User?.FullName ?? "Unknown")
                    : "MediMate",
                ReceiverName = transaction.TransactionType == "Tiền nhận vào"
                    ? "MediMate"
                    : "Bác sĩ",
                TransactionType = transaction.TransactionType,
                Content = transaction.Payment?.PaymentContent ?? "Không có nội dung",
                Amount = transaction.Payment?.Amount ?? transaction.Payout?.Amount ?? 0,
                TransactionFee = 0,
                TotalAmount = transaction.AmountPaid,
                TransactionCode = transaction.TransactionCode,
                AppointmentDate = transaction.PaidAt,
                PaymentMethod = transaction.GatewayName ?? "Chuyển khoản",
                PaymentStatus = transaction.TransactionStatus
            };

            return ApiResponse<TransactionDetailDto>.Ok(detail, "Lấy chi tiết giao dịch thành công.");
        }
    }
}
