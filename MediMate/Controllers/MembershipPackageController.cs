
using MediMate.Models.Packages;
using MediMateService.DTOs;
using MediMateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Share.Common;
using Share.Constants;

namespace MediMate.Controllers
{
    [Route("api/v1/membership-packages")]
    [ApiController]
    public class MembershipPackageController : ControllerBase
    {
        private readonly IMembershipPackageService _packageService;

        public MembershipPackageController(IMembershipPackageService packageService)
        {
            _packageService = packageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _packageService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{packageId}")]
        public async Task<IActionResult> GetById(Guid packageId)
        {
            var result = await _packageService.GetByIdAsync(packageId);
            return Ok(result);
        }

        [HttpPost]
        //[Authorize(Roles = Roles.Admin)]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateMembershipPackageRequest request)
        {
            var result = await _packageService.CreateAsync(new CreateMembershipPackageDto
            {
                PackageName = request.PackageName,
                Price = request.Price,
                Currency = request.Currency,
                DurationDays = request.DurationDays,
                MemberLimit = request.MemberLimit,
                OcrLimit = request.OcrLimit,
                ConsultantLimit = request.ConsultantLimit,
                Description = request.Description
            });
            return Ok(result);
        }

        [HttpPut("{packageId}")]
        //[Authorize(Roles = Roles.Admin)]
        [Authorize]
        public async Task<IActionResult> Update(Guid packageId, [FromBody] UpdateMembershipPackageRequest request)
        {
            var result = await _packageService.UpdateAsync(packageId, new UpdateMembershipPackageDto
            {
                PackageName = request.PackageName,
                Price = request.Price,
                Currency = request.Currency,
                DurationDays = request.DurationDays,
                MemberLimit = request.MemberLimit,
                OcrLimit = request.OcrLimit,
                ConsultantLimit = request.ConsultantLimit,
                Description = request.Description
            });
            return Ok(result);
        }

        [HttpDelete("{packageId}")]
        //[Authorize(Roles = Roles.Admin)]
        [Authorize]
        public async Task<IActionResult> Delete(Guid packageId)
        {
            var result = await _packageService.DeleteAsync(packageId);
            return Ok(result);
        }
    }
}
