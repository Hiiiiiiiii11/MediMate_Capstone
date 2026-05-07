//using MediMateService.DTOs;
//using MediMateService.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Share.Common;
//using Share.Constants;
//using Sprache;
//using System;
//using System.Threading.Tasks;

//namespace MediMate.Controllers
//{
//    [Route("api/v1/doctor-bank-accounts")]
//    [ApiController]
//    public class DoctorBankAccountController : ControllerBase
//    {
//        private readonly IDoctorBankAccountService _bankAccountService;
//        private readonly ICurrentUserService _currentUserService; // Inject Service của bạn vào đây

//        public DoctorBankAccountController(
//            IDoctorBankAccountService bankAccountService,
//            ICurrentUserService currentUserService)
//        {
//            _bankAccountService = bankAccountService;
//            _currentUserService = currentUserService;
//        }

//        [HttpPost("doctors/{doctorId}")]
//        //[Authorize(Roles = Roles.Doctor)]
//        [Authorize]
//        [ProducesResponseType(typeof(ApiResponse<DoctorBankAccountDto>), 201)]
//        public async Task<IActionResult> Create(Guid doctorId, [FromBody] CreateDoctorBankAccountRequest request)
//        {
//            try
//            {
//                // Sử dụng trực tiếp _currentUserService.UserId
//                var userId = _currentUserService.UserId; // Lấy UserId từ Service
//                var response = await _bankAccountService.CreateAsync(doctorId, userId, request);
//                if (!response.Success)
//                {
//                    return StatusCode(response.Code, response);
//                }
//                return StatusCode(201, response);

//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
//            }
//        }

//        [HttpGet("doctors/{doctorId}")]
//        //[Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.DoctorManager}")]
//        [Authorize]
//        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DoctorBankAccountDto>>), 200)]
//        public async Task<IActionResult> GetByDoctorId(Guid doctorId)
//        {
//            try
//            {
//                var response = await _bankAccountService.GetByDoctorIdAsync(doctorId);
//                return StatusCode(response.Code, response);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
//            }
//        }

//        [HttpGet("{id}")]
//        //[Authorize(Roles = $"{Roles.Doctor},{Roles.Admin},{Roles.DoctorManager}")]
//        [Authorize]
//        [ProducesResponseType(typeof(ApiResponse<DoctorBankAccountDto>), 200)]
//        public async Task<IActionResult> GetById(Guid id)
//        {
//            try
//            {
//                var response = await _bankAccountService.GetByIdAsync(id);
//                return StatusCode(response.Code, response);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
//            }
//        }

//        [HttpPut("{id}")]
//        //[Authorize(Roles = Roles.Doctor)]
//        [Authorize]
//        [ProducesResponseType(typeof(ApiResponse<DoctorBankAccountDto>), 200)]
//        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorBankAccountRequest request)
//        {
//            try
//            {
//                var response = await _bankAccountService.UpdateAsync(id, _currentUserService.UserId, request);
//                if (!response.Success)
//                    return StatusCode(response.Code, response);
//                return Ok(response);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
//            }
//        }

//        [HttpDelete("{id}")]
//        //[Authorize(Roles = Roles.Doctor)]
//        [Authorize]
//        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
//        public async Task<IActionResult> Delete(Guid id)
//        {
//            try
//            {
//                var response = await _bankAccountService.DeleteAsync(id, _currentUserService.UserId);
//                if (!response.Success)
//                    return StatusCode(response.Code, response);
//                return Ok(response);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
//            }
//        }
//    }
//}