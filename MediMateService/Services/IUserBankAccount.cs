using MediMateService.DTOs;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IUserBankAccountService
    {
        Task<UserBankAccountDto?> GetByCurrentUserAsync();
        Task<UserBankAccountDto?> GetByUserIdAsync(Guid userId); // Hàm bổ sung theo yêu cầu
        Task<ApiResponse<UserBankAccountDto>> CreateAsync(UpsertUserBankAccountRequest request);
        Task<ApiResponse<UserBankAccountDto>> UpdateAsync(UpsertUserBankAccountRequest request);
        Task<ApiResponse<object>> DeleteAsync();
    }
}
