using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.EntityFrameworkCore;
using Share.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class UserBankAccountService : IUserBankAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UserBankAccountService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<UserBankAccountDto?> GetByCurrentUserAsync()
        {
            return await GetByUserIdAsync(_currentUserService.UserId);
        }

        public async Task<UserBankAccountDto?> GetByUserIdAsync(Guid userId)
        {
            var account = await _unitOfWork.Repository<UserBankAccount>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId);

            return account != null ? MapToDto(account) : null;
        }

        public async Task<ApiResponse<UserBankAccountDto>> CreateAsync(UpsertUserBankAccountRequest request)
        {
            var userId = _currentUserService.UserId;

            var existing = await _unitOfWork.Repository<UserBankAccount>()
                .GetQueryable()
                .AnyAsync(b => b.UserId == userId);

            if (existing)
                return ApiResponse<UserBankAccountDto>.Fail("Bạn đã có thông tin ngân hàng. Dùng cập nhật để thay đổi.", 400);

            var account = new UserBankAccount
            {
                BankAccountId = Guid.NewGuid(),
                UserId = userId,
                BankName = request.BankName,
                AccountNumber = request.AccountNumber,
                AccountHolder = request.AccountHolder,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<UserBankAccount>().AddAsync(account);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<UserBankAccountDto>.Ok(MapToDto(account), "Thêm thông tin thành công.");
        }

        public async Task<ApiResponse<UserBankAccountDto>> UpdateAsync(UpsertUserBankAccountRequest request)
        {
            var userId = _currentUserService.UserId;

            var account = await _unitOfWork.Repository<UserBankAccount>()
                .GetQueryable()
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (account == null)
                return ApiResponse<UserBankAccountDto>.Fail("Không tìm thấy thông tin ngân hàng.", 404);

            account.BankName = request.BankName;
            account.AccountNumber = request.AccountNumber;
            account.AccountHolder = request.AccountHolder;
            account.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<UserBankAccount>().Update(account);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<UserBankAccountDto>.Ok(MapToDto(account), "Cập nhật thành công.");
        }

        public async Task<ApiResponse<object>> DeleteAsync()
        {
            var userId = _currentUserService.UserId;

            var account = await _unitOfWork.Repository<UserBankAccount>()
                .GetQueryable()
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (account == null)
                return ApiResponse<object>.Fail("Không tìm thấy thông tin để xóa.", 404);

            _unitOfWork.Repository<UserBankAccount>().Remove(account);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<object>.Ok(null, "Xóa thông tin thành công.");
        }

        private static UserBankAccountDto MapToDto(UserBankAccount a) => new()
        {
            BankAccountId = a.BankAccountId,
            BankName = a.BankName,
            AccountNumber = a.AccountNumber,
            AccountHolder = a.AccountHolder,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }
}
