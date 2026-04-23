using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Share.Common;
using Share.Constants;

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
            var activeSubs = await _unitOfWork.Repository<FamilySubscriptions>()
                .FindAsync(s => s.Status == "Active");

            var activeCountsByPackageId = activeSubs
                .GroupBy(s => s.PackageId)
                .ToDictionary(g => g.Key, g => g.Count());

            var result = packages
                .Select(p => MapToDto(p, activeCountsByPackageId.TryGetValue(p.PackageId, out var c) ? c : 0))
                .ToList();
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
                IsActive = dto.IsActive,
                Price = dto.Price,
                Currency = dto.Currency,
                DurationDays = dto.DurationDays,
                MemberLimit = dto.MemberLimit,
                OcrLimit = dto.OcrLimit,
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
            if (dto.IsActive.HasValue) package.IsActive = dto.IsActive.Value;
            if (dto.Price.HasValue) package.Price = dto.Price.Value;
            if (dto.Currency != null) package.Currency = dto.Currency;
            if (dto.DurationDays.HasValue) package.DurationDays = dto.DurationDays.Value;
            if (dto.MemberLimit.HasValue) package.MemberLimit = dto.MemberLimit.Value;
            if (dto.OcrLimit.HasValue) package.OcrLimit = dto.OcrLimit.Value;
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

            var activeSubs = await _unitOfWork.Repository<FamilySubscriptions>()
                .FindAsync(s => s.PackageId == packageId && s.Status == "Active");
            if (activeSubs.Any())
            {
                throw new ConflictException(
                    "Không thể xóa gói đang có người sử dụng.",
                    ErrorCodes.MembershipPackageInUse);
            }

            _unitOfWork.Repository<MembershipPackages>().Remove(package);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Xóa gói thành viên thành công.");
        }

        private static MembershipPackageDto MapToDto(MembershipPackages p, int activeSubscriberCount = 0) => new()
        {
            PackageId = p.PackageId,
            PackageName = p.PackageName,
            IsActive = p.IsActive,
            Price = p.Price,
            Currency = p.Currency,
            DurationDays = p.DurationDays,
            MemberLimit = p.MemberLimit,
            OcrLimit = p.OcrLimit,
            Description = p.Description,
            ActiveSubscriberCount = activeSubscriberCount
        };
    }
}
