using MediMate.Models.Clinics;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

namespace MediMate.Controllers
{
    [Route("api/v1/clinics")]
    [ApiController]
    [Authorize]
    public class ClinicController : ControllerBase
    {
        private readonly IClinicService _clinicService;

        public ClinicController(IClinicService clinicService)
        {
            _clinicService = clinicService;
        }

        // ─────────────────────────────────────────────
        // CLINIC CRUD
        // ─────────────────────────────────────────────

        /// <summary>Admin tạo phòng khám mới.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ClinicDto>), 201)]
        public async Task<IActionResult> CreateClinic([FromBody] CreateClinicRequest request)
        {
            var result = await _clinicService.CreateClinicAsync(new CreateClinicDto
            {
                Name = request.Name,
                Address = request.Address,
                License = request.License,
                LogoUrl = request.LogoUrl
            });
            return StatusCode(201, ApiResponse<ClinicDto>.Ok(result, "Tạo phòng khám thành công."));
        }

        /// <summary>Lấy danh sách tất cả phòng khám.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ClinicDto>>), 200)]
        public async Task<IActionResult> GetClinics([FromQuery] bool? isActive = null)
        {
            var result = await _clinicService.GetAllClinicsAsync(isActive);
            return Ok(ApiResponse<IReadOnlyList<ClinicDto>>.Ok(result, "Lấy danh sách phòng khám thành công."));
        }

        /// <summary>Lấy chi tiết một phòng khám.</summary>
        [HttpGet("{clinicId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ClinicDto>), 200)]
        public async Task<IActionResult> GetClinicById(Guid clinicId)
        {
            var result = await _clinicService.GetClinicByIdAsync(clinicId);
            return Ok(ApiResponse<ClinicDto>.Ok(result, "Lấy thông tin phòng khám thành công."));
        }

        /// <summary>Admin cập nhật thông tin phòng khám.</summary>
        [HttpPut("{clinicId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ClinicDto>), 200)]
        public async Task<IActionResult> UpdateClinic(Guid clinicId, [FromBody] UpdateClinicRequest request)
        {
            var result = await _clinicService.UpdateClinicAsync(clinicId, new UpdateClinicDto
            {
                Name = request.Name,
                Address = request.Address,
                License = request.License,
                LogoUrl = request.LogoUrl,
                IsActive = request.IsActive
            });
            return Ok(ApiResponse<ClinicDto>.Ok(result, "Cập nhật phòng khám thành công."));
        }

        /// <summary>Admin xóa phòng khám.</summary>
        [HttpDelete("{clinicId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> DeleteClinic(Guid clinicId)
        {
            await _clinicService.DeleteClinicAsync(clinicId);
            return Ok(ApiResponse<bool>.Ok(true, "Xóa phòng khám thành công."));
        }

        // ─────────────────────────────────────────────
        // CLINIC DOCTOR MANAGEMENT
        // ─────────────────────────────────────────────

        /// <summary>Lấy danh sách bác sĩ thuộc phòng khám.</summary>
        [HttpGet("{clinicId:guid}/doctors")]
        [ProducesResponseType(typeof(ApiResponse<List<ClinicDoctorDto>>), 200)]
        public async Task<IActionResult> GetDoctorsByClinic(Guid clinicId)
        {
            var result = await _clinicService.GetDoctorsByClinicAsync(clinicId);
            return Ok(ApiResponse<IReadOnlyList<ClinicDoctorDto>>.Ok(result, "Lấy danh sách bác sĩ thành công."));
        }

        /// <summary>Admin thêm bác sĩ vào phòng khám.</summary>
        [HttpPost("{clinicId:guid}/doctors")]
        [ProducesResponseType(typeof(ApiResponse<ClinicDoctorDto>), 201)]
        public async Task<IActionResult> AddDoctorToClinic(Guid clinicId, [FromBody] AddDoctorToClinicRequest request)
        {
            var result = await _clinicService.AddDoctorToClinicAsync(clinicId, new AddDoctorToClinicDto
            {
                DoctorId = request.DoctorId,
                Specialty = request.Specialty,
                ConsultationFee = request.ConsultationFee
            });
            return StatusCode(201, ApiResponse<ClinicDoctorDto>.Ok(result, "Thêm bác sĩ vào phòng khám thành công."));
        }

        /// <summary>Admin cập nhật thông tin bác sĩ trong phòng khám (giá khám, chuyên khoa).</summary>
        [HttpPut("doctors/{clinicDoctorId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ClinicDoctorDto>), 200)]
        public async Task<IActionResult> UpdateClinicDoctor(Guid clinicDoctorId, [FromBody] UpdateClinicDoctorRequest request)
        {
            var result = await _clinicService.UpdateClinicDoctorAsync(clinicDoctorId, new UpdateClinicDoctorDto
            {
                Specialty = request.Specialty,
                ConsultationFee = request.ConsultationFee,
                Status = request.Status
            });
            return Ok(ApiResponse<ClinicDoctorDto>.Ok(result, "Cập nhật thông tin bác sĩ thành công."));
        }

        /// <summary>Admin xóa (vô hiệu hóa) bác sĩ khỏi phòng khám.</summary>
        [HttpDelete("doctors/{clinicDoctorId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> RemoveDoctorFromClinic(Guid clinicDoctorId)
        {
            await _clinicService.RemoveDoctorFromClinicAsync(clinicDoctorId);
            return Ok(ApiResponse<bool>.Ok(true, "Đã xóa bác sĩ khỏi phòng khám."));
        }

        // ─────────────────────────────────────────────
        // CONTRACT MANAGEMENT
        // ─────────────────────────────────────────────

        /// <summary>Admin tạo hợp đồng mới cho phòng khám.</summary>
        [HttpPost("{clinicId:guid}/contracts")]
        [ProducesResponseType(typeof(ApiResponse<ClinicContractDto>), 201)]
        public async Task<IActionResult> CreateContract(Guid clinicId, [FromBody] CreateClinicContractRequest request)
        {
            var result = await _clinicService.CreateContractAsync(new CreateClinicContractDto
            {
                ClinicId = clinicId,
                FileUrl = request.FileUrl,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Note = request.Note
            });
            return StatusCode(201, ApiResponse<ClinicContractDto>.Ok(result, "Tạo hợp đồng thành công."));
        }

        /// <summary>Lấy danh sách hợp đồng của phòng khám.</summary>
        [HttpGet("{clinicId:guid}/contracts")]
        [ProducesResponseType(typeof(ApiResponse<List<ClinicContractDto>>), 200)]
        public async Task<IActionResult> GetContractsByClinic(Guid clinicId)
        {
            var result = await _clinicService.GetContractsByClinicAsync(clinicId);
            return Ok(ApiResponse<IReadOnlyList<ClinicContractDto>>.Ok(result, "Lấy danh sách hợp đồng thành công."));
        }

        /// <summary>Admin cập nhật trạng thái hợp đồng (Active / Expired / Terminated).</summary>
        [HttpPut("contracts/{contractId:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse<ClinicContractDto>), 200)]
        public async Task<IActionResult> UpdateContractStatus(Guid contractId, [FromBody] UpdateClinicContractStatusRequest request)
        {
            var result = await _clinicService.UpdateContractStatusAsync(contractId, request.Status);
            return Ok(ApiResponse<ClinicContractDto>.Ok(result, "Cập nhật trạng thái hợp đồng thành công."));
        }
    }
}
