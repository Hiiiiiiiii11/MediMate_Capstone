using MediMate.Models.Clinics;
 using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;

namespace MediMate.Controllers
{
    [Route("api/v1/payouts")]
    [ApiController]
    [Authorize]
    public class PayoutController : ControllerBase
    {
        private readonly IPayoutService _payoutService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public PayoutController(
            IPayoutService payoutService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _payoutService = payoutService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Admin xem danh sách tất cả các khoản công nợ (có thể filter theo clinic/status).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PayoutItemDto>>), 200)]
        public async Task<IActionResult> GetPayouts([FromQuery] PayoutFilterDto filter)
        {
            if (User.IsInRole("Doctor"))
            {
                var userId = _currentUserService.UserId;
                var doctor = (await _unitOfWork.Repository<Doctors>()
                    .FindAsync(d => d.UserId == userId)).FirstOrDefault();

                if (doctor == null)
                    return Unauthorized(ApiResponse<PagedResult<PayoutItemDto>>.Fail("Không tìm thấy thông tin bác sĩ."));

                filter.DoctorId = doctor.DoctorId;
            }

            var result = await _payoutService.GetPayoutsAsync(filter);
            return Ok(ApiResponse<PagedResult<PayoutItemDto>>.Ok(result, "Lấy danh sách công nợ thành công."));
        }

        /// <summary>
        /// Admin xem tổng hợp công nợ theo từng phòng khám — Dashboard.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<List<PayoutSummaryDto>>), 200)]
        public async Task<IActionResult> GetPayoutSummary()
        {
            var result = await _payoutService.GetPayoutSummaryByClinicAsync();
            return Ok(ApiResponse<IReadOnlyList<PayoutSummaryDto>>.Ok(result, "Lấy tổng hợp công nợ thành công."));
        }

        /// <summary>
        /// Admin xác nhận đã chuyển tiền cho phòng khám.
        /// Tất cả các khoản ReadyToPay của clinic đó sẽ được batch-update sang Paid.
        /// </summary>
        [HttpPost("clinics/{clinicId:guid}/process")]
        [ProducesResponseType(typeof(ApiResponse<int>), 200)]
        public async Task<IActionResult> ProcessClinicPayout(Guid clinicId, [FromForm] ProcessPayoutRequest request)
        {
            var count = await _payoutService.ProcessClinicPayoutAsync(clinicId, new ProcessPayoutDto
            {
                TransferImage = request.TransferImage,
                ReportFile = request.ReportFile,
                Note = request.Note
            });
            return Ok(ApiResponse<int>.Ok(count, $"Đã xác nhận thanh toán thành công cho {count} khoản công nợ."));
        }
    }
}
