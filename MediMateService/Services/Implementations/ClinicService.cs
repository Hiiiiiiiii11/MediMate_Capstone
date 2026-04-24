using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace MediMateService.Services.Implementations
{
    public class ClinicService : IClinicService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadPhotoService _uploadPhotoService;

        public ClinicService(IUnitOfWork unitOfWork, IUploadPhotoService  uploadPhotoService)
        {
            _unitOfWork = unitOfWork;
            _uploadPhotoService = uploadPhotoService;
        }

        // ─────────────────────────────────────────────────────────────────
        // CLINIC CRUD
        // ─────────────────────────────────────────────────────────────────

        public async Task<ClinicDto> CreateClinicAsync(CreateClinicDto dto)
        {
            string logoUrl = string.Empty;
            string LicenseUrl = string.Empty;

            // 1. Upload lên Cloudinary nếu có file
            if (dto.LogoFile != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(dto.LogoFile);

                // Truy cập vào thuộc tính Url (hoặc SecureUrl tùy theo DTO của bạn)
                logoUrl = uploadResult.OriginalUrl;
            }
            if (dto.LicenseFile != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(dto.LicenseFile);

                // Truy cập vào thuộc tính Url (hoặc SecureUrl tùy theo DTO của bạn)
                LicenseUrl = uploadResult.OriginalUrl;
            }

            // 2. Lưu vào DB
            var clinic = new Clinics
            {
                Name = dto.Name,
                Address = dto.Address,
                LicenseUrl = LicenseUrl,
                LogoUrl = logoUrl, // URL từ Cloudinary
                IsActive = true,
                Email = dto.Email,
                BankName = dto.BankName,
                BankAccountNumber = dto.BankAccountNumber,
                BankAccountHolder = dto.BankAccountHolder,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<Clinics>().AddAsync(clinic);
            await _unitOfWork.CompleteAsync();

            return MapClinicDto(clinic, 0);
        }

        public async Task<ClinicDto> GetClinicByIdAsync(Guid clinicId)
        {
            var clinic = await _unitOfWork.Repository<Clinics>().GetQueryable()
                .Include(c => c.ClinicDoctors)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClinicId == clinicId);

            if (clinic == null)
                throw new NotFoundException($"Không tìm thấy phòng khám với ID {clinicId}.");

            return MapClinicDto(clinic, clinic.ClinicDoctors.Count(d => d.Status == "Active"));
        }

        public async Task<IReadOnlyList<ClinicDto>> GetAllClinicsAsync(bool? isActive = null)
        {
            var query = _unitOfWork.Repository<Clinics>().GetQueryable()
                .Include(c => c.ClinicDoctors)
                .AsNoTracking();

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            var clinics = await query.OrderBy(c => c.Name).ToListAsync();

            return clinics.Select(c => MapClinicDto(c, c.ClinicDoctors.Count(d => d.Status == "Active"))).ToList();
        }

        public async Task<ClinicDto> UpdateClinicAsync(Guid clinicId, UpdateClinicDto dto)
        {
            var clinic = await _unitOfWork.Repository<Clinics>().GetQueryable()
                .Include(c => c.ClinicDoctors)
                .FirstOrDefaultAsync(c => c.ClinicId == clinicId);

            if (clinic == null)
                throw new NotFoundException($"Không tìm thấy phòng khám với ID {clinicId}.");

            if (dto.Name != null) clinic.Name = dto.Name;
            if (dto.Address != null) clinic.Address = dto.Address;
            if (dto.Email != null) clinic.Email = dto.Email;
            if (dto.LicenseFile != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(dto.LicenseFile);

                // Truy cập vào thuộc tính Url (hoặc SecureUrl tùy theo DTO của bạn)
                clinic.LicenseUrl = uploadResult.OriginalUrl;
            }
            if (dto.LogoFile != null)
            {
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(dto.LogoFile);

                // Truy cập vào thuộc tính Url (hoặc SecureUrl tùy theo DTO của bạn)
                clinic.LogoUrl = uploadResult.OriginalUrl;
            }
            if (dto.IsActive.HasValue) clinic.IsActive = dto.IsActive.Value;

            // ── Cập nhật thông tin ngân hàng nếu có ──
            if (dto.BankName != null) clinic.BankName = dto.BankName;
            if (dto.BankAccountNumber != null) clinic.BankAccountNumber = dto.BankAccountNumber;
            if (dto.BankAccountHolder != null) clinic.BankAccountHolder = dto.BankAccountHolder;

            clinic.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<Clinics>().Update(clinic);
            await _unitOfWork.CompleteAsync();

            return MapClinicDto(clinic, clinic.ClinicDoctors.Count(d => d.Status == "Active"));
        }

        public async Task DeleteClinicAsync(Guid clinicId)
        {
            var clinic = await _unitOfWork.Repository<Clinics>().GetByIdAsync(clinicId);
            if (clinic == null)
                throw new NotFoundException($"Không tìm thấy phòng khám với ID {clinicId}.");

            _unitOfWork.Repository<Clinics>().Remove(clinic);
            await _unitOfWork.CompleteAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // CLINIC DOCTOR MANAGEMENT
        // ─────────────────────────────────────────────────────────────────

        public async Task<ClinicDoctorDto> AddDoctorToClinicAsync(Guid clinicId, AddDoctorToClinicDto dto)
        {
            // Kiểm tra phòng khám tồn tại
            var clinic = await _unitOfWork.Repository<Clinics>().GetByIdAsync(clinicId)
                ?? throw new NotFoundException($"Không tìm thấy phòng khám với ID {clinicId}.");

            // Kiểm tra bác sĩ tồn tại
            var doctor = await _unitOfWork.Repository<Doctors>().GetByIdAsync(dto.DoctorId)
                ?? throw new NotFoundException($"Không tìm thấy bác sĩ với ID {dto.DoctorId}.");

            // Kiểm tra bác sĩ chưa thuộc phòng khám này
            var existing = await _unitOfWork.Repository<ClinicDoctors>().GetQueryable()
                .FirstOrDefaultAsync(cd => cd.ClinicId == clinicId && cd.DoctorId == dto.DoctorId && cd.Status == "Active");

            if (existing != null)
                throw new ConflictException("Bác sĩ này đã thuộc phòng khám.");

            var clinicDoctor = new ClinicDoctors
            {
                ClinicId = clinicId,
                DoctorId = dto.DoctorId,
                Specialty = dto.Specialty,
                ConsultationFee = dto.ConsultationFee,
                Status = "Active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<ClinicDoctors>().AddAsync(clinicDoctor);
            await _unitOfWork.CompleteAsync();

            return new ClinicDoctorDto
            {
                Id = clinicDoctor.Id,
                ClinicId = clinicId,
                ClinicName = clinic.Name,
                DoctorId = dto.DoctorId,
                DoctorName = doctor.FullName,
                DoctorAvatar = null,
                Specialty = dto.Specialty,
                ConsultationFee = dto.ConsultationFee,
                Status = "Active",
                CreatedAt = clinicDoctor.CreatedAt
            };
        }

        public async Task<ClinicDoctorDto> UpdateClinicDoctorAsync(Guid clinicDoctorId, UpdateClinicDoctorDto dto)
        {
            var cd = await _unitOfWork.Repository<ClinicDoctors>().GetQueryable()
                .Include(x => x.Clinic)
                .Include(x => x.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(x => x.Id == clinicDoctorId);

            if (cd == null)
                throw new NotFoundException("Không tìm thấy liên kết bác sĩ - phòng khám.");

            if (dto.Specialty != null) cd.Specialty = dto.Specialty;
            if (dto.ConsultationFee.HasValue) cd.ConsultationFee = dto.ConsultationFee.Value;
            if (dto.Status != null) cd.Status = dto.Status;
            cd.UpdatedAt = DateTime.Now;

            _unitOfWork.Repository<ClinicDoctors>().Update(cd);
            await _unitOfWork.CompleteAsync();

            return new ClinicDoctorDto
            {
                Id = cd.Id,
                ClinicId = cd.ClinicId,
                ClinicName = cd.Clinic?.Name ?? string.Empty,
                DoctorId = cd.DoctorId,
                DoctorName = cd.Doctor?.User?.FullName ?? cd.Doctor?.FullName ?? string.Empty,
                DoctorAvatar = cd.Doctor?.User?.AvatarUrl,
                Specialty = cd.Specialty,
                ConsultationFee = cd.ConsultationFee,
                Status = cd.Status,
                CreatedAt = cd.CreatedAt
            };
        }

        public async Task RemoveDoctorFromClinicAsync(Guid clinicDoctorId)
        {
            var cd = await _unitOfWork.Repository<ClinicDoctors>().GetByIdAsync(clinicDoctorId)
                ?? throw new NotFoundException("Không tìm thấy liên kết bác sĩ - phòng khám.");

            // Soft delete: đổi status thành Inactive thay vì xóa cứng
            cd.Status = "Inactive";
            cd.UpdatedAt = DateTime.Now;
            _unitOfWork.Repository<ClinicDoctors>().Update(cd);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IReadOnlyList<ClinicDoctorDto>> GetDoctorsByClinicAsync(Guid clinicId)
        {
            var list = await _unitOfWork.Repository<ClinicDoctors>().GetQueryable()
                .Include(cd => cd.Clinic)
                .Include(cd => cd.Doctor).ThenInclude(d => d.User)
                .AsNoTracking()
                .Where(cd => cd.ClinicId == clinicId && cd.Status == "Active")
                .OrderBy(cd => cd.CreatedAt)
                .ToListAsync();

            return list.Select(cd => new ClinicDoctorDto
            {
                Id = cd.Id,
                ClinicId = cd.ClinicId,
                ClinicName = cd.Clinic?.Name ?? string.Empty,
                DoctorId = cd.DoctorId,
                DoctorName = cd.Doctor?.User?.FullName ?? cd.Doctor?.FullName ?? string.Empty,
                DoctorAvatar = cd.Doctor?.User?.AvatarUrl,
                Specialty = cd.Specialty,
                ConsultationFee = cd.ConsultationFee,
                Status = cd.Status,
                CreatedAt = cd.CreatedAt
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────
        // CLINIC CONTRACT MANAGEMENT
        // ─────────────────────────────────────────────────────────────────

        public async Task<ClinicContractDto> CreateContractAsync(CreateClinicContractDto dto)
        {
            var clinic = await _unitOfWork.Repository<Clinics>().GetByIdAsync(dto.ClinicId)
                ?? throw new NotFoundException($"Không tìm thấy phòng khám với ID {dto.ClinicId}.");

            string uploadedFileUrl = string.Empty;
            if (dto.ContractFile != null && dto.ContractFile.Length > 0)
            {
                // VÌ TRẢ VỀ STRING NÊN GÁN THẲNG LUÔN, KHÔNG DÙNG .Url NỮA
                uploadedFileUrl = await _uploadPhotoService.UploadDocumentAsync(dto.ContractFile);
            }
            else
            {
                throw new BadRequestException("File hợp đồng không được để trống.");
            }

            // 3. Khởi tạo đối tượng Contract với URL đã upload
            var contract = new ClinicContract
            {
                ClinicId = dto.ClinicId,
                FileUrl = uploadedFileUrl, // Gán URL từ Cloudinary vào đây
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = "Active",
                Note = dto.Note,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Repository<ClinicContract>().AddAsync(contract);
            await _unitOfWork.CompleteAsync();

            return new ClinicContractDto
            {
                ContractId = contract.ContractId,
                ClinicId = contract.ClinicId,
                ClinicName = clinic.Name,
                FileUrl = contract.FileUrl,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = contract.Status,
                Note = contract.Note,
                CreatedAt = contract.CreatedAt
            };
        }

        public async Task<IReadOnlyList<ClinicContractDto>> GetContractsByClinicAsync(Guid clinicId)
        {
            var contracts = await _unitOfWork.Repository<ClinicContract>().GetQueryable()
                .Include(c => c.Clinic)
                .AsNoTracking()
                .Where(c => c.ClinicId == clinicId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return contracts.Select(c => new ClinicContractDto
            {
                ContractId = c.ContractId,
                ClinicId = c.ClinicId,
                ClinicName = c.Clinic?.Name ?? string.Empty,
                FileUrl = c.FileUrl,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                Note = c.Note,
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task<ClinicContractDto> UpdateContractStatusAsync(Guid contractId, string status)
        {
            var contract = await _unitOfWork.Repository<ClinicContract>().GetQueryable()
                .Include(c => c.Clinic)
                .FirstOrDefaultAsync(c => c.ContractId == contractId)
                ?? throw new NotFoundException("Không tìm thấy hợp đồng.");

            contract.Status = status;
            contract.UpdatedAt = DateTime.Now;
            _unitOfWork.Repository<ClinicContract>().Update(contract);
            await _unitOfWork.CompleteAsync();

            return new ClinicContractDto
            {
                ContractId = contract.ContractId,
                ClinicId = contract.ClinicId,
                ClinicName = contract.Clinic?.Name ?? string.Empty,
                FileUrl = contract.FileUrl,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = contract.Status,
                Note = contract.Note,
                CreatedAt = contract.CreatedAt
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────────────

        private static ClinicDto MapClinicDto(Clinics clinic, int doctorCount) => new()
        {
            ClinicId = clinic.ClinicId,
            Name = clinic.Name,
            Address = clinic.Address,
            LicenseUrl = clinic.LicenseUrl,
            LogoUrl = clinic.LogoUrl,
            IsActive = clinic.IsActive,
            CreatedAt = clinic.CreatedAt,
            DoctorCount = doctorCount,
            Email = clinic.Email,
            BankName = clinic.BankName,
            BankAccountNumber = clinic.BankAccountNumber,
            BankAccountHolder = clinic.BankAccountHolder
        };
    }
}
