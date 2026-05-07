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
    [ApiController]
    [Route("api/v1/user/bank-account")]
    [Authorize]
    public class UserBankAccountController : ControllerBase
    {
        private readonly IUserBankAccountService _bankAccountService;

        public UserBankAccountController(IUserBankAccountService bankAccountService)
        {
            _bankAccountService = bankAccountService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserBankAccountDto>), 200)]
        public async Task<IActionResult> Get()
        {
            var result = await _bankAccountService.GetByCurrentUserAsync();
            if (result == null)
                return Ok(ApiResponse<UserBankAccountDto>.Ok(null, "Bạn chưa có thông tin ngân hàng."));

            return Ok(ApiResponse<UserBankAccountDto>.Ok(result, "Lấy thông tin thành công."));
        }

        // Endpoint dành cho Admin/System truy vấn theo UserId
        [HttpGet("by-user/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<UserBankAccountDto>), 200)]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var result = await _bankAccountService.GetByUserIdAsync(userId);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Người dùng này chưa cập nhật ngân hàng.", 404));

            return Ok(ApiResponse<UserBankAccountDto>.Ok(result));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserBankAccountDto>), 200)]
        public async Task<IActionResult> Create([FromBody] UpsertUserBankAccountRequest request)
        {
            var response = await _bankAccountService.CreateAsync(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<UserBankAccountDto>), 200)]
        public async Task<IActionResult> Update([FromBody] UpsertUserBankAccountRequest request)
        {
            var response = await _bankAccountService.UpdateAsync(request);
            return response.Success ? Ok(response) : NotFound(response);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Delete()
        {
            var response = await _bankAccountService.DeleteAsync();
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}
