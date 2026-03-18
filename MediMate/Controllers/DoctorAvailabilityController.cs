using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;
using System;
using System.Threading.Tasks;
using static Google.Apis.Requests.BatchRequest;

namespace MediMate.Controllers
{
    [Route("api/v1/doctor-availabilities")]
    [ApiController]
    public class DoctorAvailabilityController : ControllerBase
    {
        private readonly IDoctorAvailabilityService _availabilityService;
        private readonly ICurrentUserService _currentUserService;

        public DoctorAvailabilityController(
            IDoctorAvailabilityService availabilityService,
            ICurrentUserService currentUserService)
        {
            _availabilityService = availabilityService;
            _currentUserService = currentUserService;
        }

        [HttpPost("doctors/{doctorId}")]
        //[Authorize(Roles = Roles.Doctor)]
        [Authorize]
        public async Task<IActionResult> CreateAvailabilities(Guid doctorId, [FromBody] List<CreateDoctorAvailabilityRequest> request)
        {
            try
            {
                // 1. Tạo 1 danh sách rỗng để hứng dữ liệu thành công
                var createdResults = new List<object>();

                foreach (var item in request)
                {
                    var response = await _availabilityService.CreateAsync(doctorId, _currentUserService.UserId, item);

                    // Nếu có 1 ca bị lỗi (ví dụ: trùng giờ), dừng ngay lập tức và báo lỗi
                    if (!response.Success)
                    {
                        return StatusCode(response.Code, response);
                    }

                    // Nếu thành công, nhét dữ liệu vào danh sách
                    createdResults.Add(response.Data);
                }

                // 2. Trả về toàn bộ danh sách đã tạo thành công sau khi vòng lặp kết thúc
                var finalResponse = ApiResponse<object>.Ok(createdResults, "Tạo lịch làm việc thành công.");
                return StatusCode(201, finalResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // Bất kỳ ai (User, Doctor, Admin) cũng có thể xem lịch để đặt khám
        [HttpGet("doctors/{doctorId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDoctorId(Guid doctorId)
        {
            try
            {
                var response = await _availabilityService.GetByDoctorIdAsync(doctorId);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var response = await _availabilityService.GetByIdAsync(id);
                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = Roles.Doctor)]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorAvailabilityRequest request)
        {
            try
            {
                var response = await _availabilityService.UpdateAsync(id, _currentUserService.UserId, request);
                if (!response.Success) return StatusCode(response.Code, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = Roles.Doctor)]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var response = await _availabilityService.DeleteAsync(id, _currentUserService.UserId);
                if (!response.Success) return StatusCode(response.Code, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}