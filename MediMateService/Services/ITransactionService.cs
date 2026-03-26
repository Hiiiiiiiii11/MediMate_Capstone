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
    }
}
