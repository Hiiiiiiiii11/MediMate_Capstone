using MediMateService.DTOs;
using Share.Common;
using System;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface ITransactionService
    {
        Task<ApiResponse<PagedResult<TransactionItemDto>>> GetAllTransactionsAsync(TransactionFilterDto filter);
        Task<ApiResponse<TransactionDetailDto>> GetTransactionDetailAsync(Guid transactionId);
        Task<ApiResponse<TransactionDetailDto>> GetTransactionByPaymentIdAsync(Guid paymentId);
        Task<ApiResponse<PagedResult<TransactionItemDto>>> GetTransactionsByUserIdAsync(Guid userId, TransactionFilterDto filter);
        Task<ApiResponse<bool>> UpdateTransactionStatusAsync(Guid transactionId, string status);
        Task<ApiResponse<bool>> ApproveDoctorPayoutAsync(Guid payoutId, ApprovePayoutRequest request);
        Task<ApiResponse<PagedResult<PendingPayoutDto>>> GetPendingPayoutsAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null);
        Task<ApiResponse<PagedResult<PaidPayoutDto>>> GetPaidPayoutsAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null);
    }
}
