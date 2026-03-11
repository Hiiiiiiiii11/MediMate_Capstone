using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Share.Common;

namespace MediMateService.Services.Implementations
{
    public class MembershipPackageService : IMembershipPackageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MembershipPackageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<List<MembershipPackageDto>>> GetAllAsync()
        {
            var packages = await _unitOfWork.Repository<MembershipPackages>().GetAllAsync();
            var result = packages.Select(MapToDto).ToList();
            return ApiResponse<List<MembershipPackageDto>>.Ok(result, "Lấy danh sách gói thành viên thành công.");
        }

        public async Task<ApiResponse<MembershipPackageDto>> GetByIdAsync(Guid packageId)
        {
            var package = await _unitOfWork.Repository<MembershipPackages>().GetByIdAsync(packageId);
            if (package == null)
                throw new NotFoundException("Không tìm thấy gói thành viên.");

            return ApiResponse<MembershipPackageDto>.Ok(MapToDto(package), "Lấy thông tin gói thành viên thành công.");
        }

        public async Task<ApiResponse<MembershipPackageDto>> CreateAsync(CreateMembershipPackageDto dto)
        {
            var package = new MembershipPackages
            {
                PackageId = Guid.NewGuid(),
                PackageName = dto.PackageName,
                Price = dto.Price,
                Currency = dto.Currency,
                DurationDays = dto.DurationDays,
                MemberLimit = dto.MemberLimit,
                Description = dto.Description
            };

            await _unitOfWork.Repository<MembershipPackages>().AddAsync(package);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<MembershipPackageDto>.Ok(MapToDto(package), "Tạo gói thành viên thành công.");
        }

        public async Task<ApiResponse<MembershipPackageDto>> UpdateAsync(Guid packageId, UpdateMembershipPackageDto dto)
        {
            var package = await _unitOfWork.Repository<MembershipPackages>().GetByIdAsync(packageId);
            if (package == null)
                throw new NotFoundException("Không tìm thấy gói thành viên.");

            if (dto.PackageName != null) package.PackageName = dto.PackageName;
            if (dto.Price.HasValue) package.Price = dto.Price.Value;
            if (dto.Currency != null) package.Currency = dto.Currency;
            if (dto.DurationDays.HasValue) package.DurationDays = dto.DurationDays.Value;
            if (dto.MemberLimit.HasValue) package.MemberLimit = dto.MemberLimit.Value;
            if (dto.Description != null) package.Description = dto.Description;

            _unitOfWork.Repository<MembershipPackages>().Update(package);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<MembershipPackageDto>.Ok(MapToDto(package), "Cập nhật gói thành viên thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid packageId)
        {
            var package = await _unitOfWork.Repository<MembershipPackages>().GetByIdAsync(packageId);
            if (package == null)
                throw new NotFoundException("Không tìm thấy gói thành viên.");

            _unitOfWork.Repository<MembershipPackages>().Remove(package);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa gói thành viên thành công.");
        }

        private static MembershipPackageDto MapToDto(MembershipPackages p) => new()
        {
            PackageId = p.PackageId,
            PackageName = p.PackageName,
            Price = p.Price,
            Currency = p.Currency,
            DurationDays = p.DurationDays,
            MemberLimit = p.MemberLimit,
            Description = p.Description
        };
    }
}
