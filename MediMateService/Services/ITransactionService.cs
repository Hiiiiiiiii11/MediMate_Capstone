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
    }
}
