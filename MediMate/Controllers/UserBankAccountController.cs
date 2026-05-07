using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Share.Common;
using System.ComponentModel.DataAnnotations;

namespace MediMate.Controllers
{
    /// <summary>
    /// Quản lý thông tin ngân hàng của User — phục vụ hoàn tiền khi hủy lịch hẹn.
    /// </summary>
    [ApiController]
    [Route("api/v1/user/bank-account")]
    [Authorize]
    public class UserBankAccountController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public UserBankAccountController(ICurrentUserService currentUserService, IUnitOfWork unitOfWork)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        // ─────────────────────────────────────────────────────────────────
        // GET: Lấy thông tin ngân hàng của User đang đăng nhập
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserBankAccountDto>), 200)]
        public async Task<IActionResult> Get()
        {
            var userId = _currentUserService.UserId;
            var account = await _unitOfWork.Repository<UserBankAccount>()
                .GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (account == null)
                return Ok(ApiResponse<UserBankAccountDto>.Ok(null, "Bạn chưa có thông tin ngân hàng."));

            return Ok(ApiResponse<UserBankAccountDto>.Ok(MapDto(account), "Lấy thông tin ngân hàng thành công."));
        }

        // ─────────────────────────────────────────────────────────────────
        // POST: Thêm thông tin ngân hàng
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserBankAccountDto>), 200)]
        public async Task<IActionResult> Create([FromBody] UpsertUserBankAccountRequest request)
        {
            var userId = _currentUserService.UserId;

            // Mỗi User chỉ có 1 tài khoản — kiểm tra đã tồn tại chưa
            var existing = await _unitOfWork.Repository<UserBankAccount>()
                .GetQueryable()
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (existing != null)
                return BadRequest(ApiResponse<object>.Fail(
                    "Bạn đã có thông tin ngân hàng. Dùng PUT để cập nhật.", 400));

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

            return Ok(ApiResponse<UserBankAccountDto>.Ok(MapDto(account), "Thêm thông tin ngân hàng thành công."));
        }

        // ─────────────────────────────────────────────────────────────────
        // PUT: Cập nhật thông tin ngân hàng
        // ─────────────────────────────────────────────────────────────────
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<UserBankAccountDto>), 200)]
        public async Task<IActionResult> Update([FromBody] UpsertUserBankAccountRequest request)
        {
            var userId = _currentUserService.UserId;

            var account = await _unitOfWork.Repository<UserBankAccount>()
                .GetQueryable()
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (account == null)
                return NotFound(ApiResponse<object>.Fail("Bạn chưa có thông tin ngân hàng. Dùng POST để tạo mới.", 404));

            account.BankName = request.BankName;
            account.AccountNumber = request.AccountNumber;
            account.AccountHolder = request.AccountHolder;
            account.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<UserBankAccount>().Update(account);
            await _unitOfWork.CompleteAsync();

            return Ok(ApiResponse<UserBankAccountDto>.Ok(MapDto(account), "Cập nhật thông tin ngân hàng thành công."));
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE: Xóa thông tin ngân hàng
        // ─────────────────────────────────────────────────────────────────
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Delete()
        {
            var userId = _currentUserService.UserId;

            var account = await _unitOfWork.Repository<UserBankAccount>()
                .GetQueryable()
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (account == null)
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin ngân hàng.", 404));

            _unitOfWork.Repository<UserBankAccount>().Remove(account);
            await _unitOfWork.CompleteAsync();

            return Ok(ApiResponse<object>.Ok(null, "Đã xóa thông tin ngân hàng."));
        }

        private static UserBankAccountDto MapDto(UserBankAccount a) => new()
        {
            BankAccountId = a.BankAccountId,
            BankName = a.BankName,
            AccountNumber = a.AccountNumber,
            AccountHolder = a.AccountHolder,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }

    // ─── Request / Response Models ─────────────────────────────────────────
    public class UpsertUserBankAccountRequest
    {
        [Required] public string BankName { get; set; } = string.Empty;
        [Required] public string AccountNumber { get; set; } = string.Empty;
        [Required] public string AccountHolder { get; set; } = string.Empty;
    }

    public class UserBankAccountDto
    {
        public Guid BankAccountId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
