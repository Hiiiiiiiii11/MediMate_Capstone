using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Share.Common;
using Share.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediMateService.Services.Implementations
{
    public class DoctorBankAccountService : IDoctorBankAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DoctorBankAccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<DoctorBankAccountDto>> CreateAsync(Guid doctorId, Guid currentUserId, CreateDoctorBankAccountRequest request)
        {
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(doctorId);
            if (doctor == null)
                return ApiResponse<DoctorBankAccountDto>.Fail("Không tìm thấy thông tin bác sĩ.", 404);

            // Chỉ bác sĩ chủ tài khoản mới được quyền thêm ngân hàng
            if (doctor.UserId != currentUserId)
                return ApiResponse<DoctorBankAccountDto>.Fail("Bạn không có quyền thêm tài khoản cho bác sĩ này.", 403);

            var bankAccount = new DoctorBankAccount
            {
                BankAccountId = Guid.NewGuid(),
                DoctorId = doctorId,
                BankName = request.BankName,
                AccountNumber = request.AccountNumber,
                AccountHolder = request.AccountHolder,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<DoctorBankAccount>().AddAsync(bankAccount);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorBankAccountDto>.Ok(MapToDto(bankAccount), "Thêm tài khoản ngân hàng thành công.");
        }

        public async Task<ApiResponse<IEnumerable<DoctorBankAccountDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            var accounts = await _unitOfWork.Repository<DoctorBankAccount>()
                .FindAsync(b => b.DoctorId == doctorId);

            var response = accounts.OrderByDescending(b => b.CreatedAt).Select(MapToDto);
            return ApiResponse<IEnumerable<DoctorBankAccountDto>>.Ok(response);
        }

        public async Task<ApiResponse<DoctorBankAccountDto>> GetByIdAsync(Guid bankAccountId)
        {
            var account = await _unitOfWork.Repository<DoctorBankAccount>().GetByIdAsync(bankAccountId);
            if (account == null)
                return ApiResponse<DoctorBankAccountDto>.Fail("Không tìm thấy tài khoản ngân hàng.", 404);

            return ApiResponse<DoctorBankAccountDto>.Ok(MapToDto(account));
        }

        public async Task<ApiResponse<DoctorBankAccountDto>> UpdateAsync(Guid bankAccountId, Guid currentUserId, UpdateDoctorBankAccountRequest request)
        {
            var account = (await _unitOfWork.Repository<DoctorBankAccount>()
                .FindAsync(b => b.BankAccountId == bankAccountId, "Doctor")).FirstOrDefault();

            if (account == null)
                return ApiResponse<DoctorBankAccountDto>.Fail("Không tìm thấy tài khoản ngân hàng.", 404);

            if (account.Doctor.UserId != currentUserId)
                return ApiResponse<DoctorBankAccountDto>.Fail("Bạn không có quyền sửa tài khoản này.", 403);

            account.BankName = request.BankName;
            account.AccountNumber = request.AccountNumber;
            account.AccountHolder = request.AccountHolder;

            _unitOfWork.Repository<DoctorBankAccount>().Update(account);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<DoctorBankAccountDto>.Ok(MapToDto(account), "Cập nhật tài khoản thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid bankAccountId, Guid currentUserId)
        {
            var account = (await _unitOfWork.Repository<DoctorBankAccount>()
                .FindAsync(b => b.BankAccountId == bankAccountId, "Doctor")).FirstOrDefault();

            if (account == null)
                return ApiResponse<bool>.Fail("Không tìm thấy tài khoản ngân hàng.", 404);

            if (account.Doctor.UserId != currentUserId)
                return ApiResponse<bool>.Fail("Bạn không có quyền xóa tài khoản này.", 403);

            _unitOfWork.Repository<DoctorBankAccount>().Remove(account);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa tài khoản thành công.");
        }

        private DoctorBankAccountDto MapToDto(DoctorBankAccount b)
        {
            return new DoctorBankAccountDto
            {
                BankAccountId = b.BankAccountId,
                DoctorId = b.DoctorId,
                BankName = b.BankName,
                AccountNumber = b.AccountNumber,
                AccountHolder = b.AccountHolder,
                CreatedAt = b.CreatedAt
            };
        }
    }
}