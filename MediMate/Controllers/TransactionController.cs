using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using System;
using System.Threading.Tasks;

namespace MediMate.Controllers
{
    [Route("api/v1/transactions")]
    [ApiController]
    [Authorize] // Có thể thêm Roles = Admin nếu chỉ Admin được quản lý
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TransactionItemDto>>), 200)]
        public async Task<IActionResult> GetAllTransactions([FromQuery] TransactionFilterDto filter)
        {
            try
            {
                var result = await _transactionService.GetAllTransactionsAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TransactionDetailDto>), 200)]
        public async Task<IActionResult> GetTransactionDetail(Guid id)
        {
            try
            {
                var result = await _transactionService.GetTransactionDetailAsync(id);
                if (!result.Success) return StatusCode(result.Code, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpGet("{paymentId}/transaction")]
        [ProducesResponseType(typeof(ApiResponse<TransactionDetailDto>), 200)]
        [Authorize]
        public async Task<IActionResult> GetTransactionByPaymentId(Guid paymentId)
        {
            try
            {
                var result = await _transactionService.GetTransactionByPaymentIdAsync(paymentId);

                if (result.Success) return Ok(result);
                return NotFound(result);
                // Trả về 404 nếu không tìm thấy
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TransactionItemDto>>), 200)]
        public async Task<IActionResult> GetTransactionByUserId(Guid userId, [FromQuery] TransactionFilterDto filter)
        {
            try
            {
                // Đã bổ sung biến filter vào hàm gọi Service
                var result = await _transactionService.GetTransactionsByUserIdAsync(userId, filter);

                if (result.Success) return Ok(result);
                return NotFound(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Lỗi hệ thống: " + ex.Message, 500));
            }
        }
        [HttpPut("{transactionId}/status")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [Authorize]
        public async Task<IActionResult> UpdateTransactionStatus(Guid transactionId, [FromBody] UpdateTransactionStatusRequest request)
        {
            try
            {
                // Chuẩn hóa chuỗi gửi lên (xóa khoảng trắng và in hoa)
                var upperStatus = request.Status?.Trim().ToUpper() ?? "";

                // Danh sách các trạng thái hợp lệ (hỗ trợ cả tiếng Anh Anh và Anh Mỹ cho chữ Cancelled)
                var allowedStatuses = new[] { "SUCCESS", "FAILED", "CANCELED", "CANCELLED", "PENDING" };

                if (!allowedStatuses.Contains(upperStatus))
                {
                    return BadRequest(ApiResponse<bool>.Fail(
                        $"Trạng thái '{request.Status}' không hợp lệ. Chỉ chấp nhận: SUCCESS, FAILED, CANCELED, PENDING.", 400));
                }

                // Chuyển CANCELED (1 chữ L) thành CANCELLED (2 chữ L) để đồng bộ DB nếu cần
                if (upperStatus == "CANCELED")
                {
                    upperStatus = "CANCELLED";
                }

                // Gọi Service (Service của bạn đã có sẵn logic format lại thành "Success", "Failed"...)
                var result = await _transactionService.UpdateTransactionStatusAsync(transactionId, upperStatus);

                if (result.Success)
                {
                    return Ok(result);
                }

                return StatusCode(result.Code, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.Fail("Lỗi hệ thống: " + ex.Message, 500));
            }
        }
    }

}
