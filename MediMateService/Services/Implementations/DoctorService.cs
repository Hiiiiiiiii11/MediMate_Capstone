using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class DoctorService : IDoctorService
    {
        private readonly IMockDoctorRepository _repo;

        public DoctorService(IMockDoctorRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<DoctorDto>> GetPublicDoctorsAsync(string? specialty = null)
        {
            var list = await _repo.GetPublicDoctorsAsync();
            if (!string.IsNullOrWhiteSpace(specialty))
            {
                list = list.Where(d => d.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return list.Select(MapToDto).ToList();
        }

        public async Task<DoctorDto> GetPublicDoctorByIdAsync(Guid doctorId)
        {
            var doc = await _repo.GetPublicDoctorByIdAsync(doctorId);
            return doc == null ? throw new NotFoundException("Không tìm thấy bác sĩ.") : MapToDto(doc);
        }

        public async Task<List<DoctorAvailabilityDto>> GetPublicAvailabilityByDoctorAsync(Guid doctorId)
        {
            var doc = await _repo.GetPublicDoctorByIdAsync(doctorId);
            if (doc == null)
            {
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            }

            var list = (await _repo.GetAvailabilityByDoctorIdAsync(doctorId))
                .Where(a => a.IsActive)
                .ToList();
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<DoctorDto>> GetDoctorsAsync(string? specialty = null)
        {
            var list = await _repo.GetAllDoctorsAsync();
            if (!string.IsNullOrWhiteSpace(specialty))
            {
                list = list.Where(d => d.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            var dtos = list.Select(MapToDto).ToList();
            return dtos;
        }

        public async Task<DoctorDto> GetDoctorByIdAsync(Guid doctorId)
        {
            var doc = await _repo.GetDoctorByIdAsync(doctorId);
            return doc == null ? throw new NotFoundException("Không tìm thấy bác sĩ.") : MapToDto(doc);
        }

        public async Task<List<DoctorAvailabilityDto>> GetAvailabilityByDoctorAsync(Guid doctorId)
        {
            var doc = await _repo.GetDoctorByIdAsync(doctorId);
            if (doc == null)
            {
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            }

            var list = await _repo.GetAvailabilityByDoctorIdAsync(doctorId);
            var dtos = list.Select(MapToDto).ToList();
            return dtos;
        }

        public async Task<List<DoctorAvailabilityExceptionDto>> GetExceptionsByDoctorAsync(Guid doctorId)
        {
            var doc = await _repo.GetDoctorByIdAsync(doctorId);
            if (doc == null)
            {
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            }

            var list = await _repo.GetExceptionsByDoctorIdAsync(doctorId);
            var dtos = list.Select(MapToDto).ToList();
            return dtos;
        }

        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto request)
        {
            var existedLicense = (await _repo.GetAllDoctorsAsync())
                .Any(d => d.LicenseNumber.Equals(request.LicenseNumber, StringComparison.OrdinalIgnoreCase));
            if (existedLicense)
            {
                throw new ConflictException("Số giấy phép hành nghề đã tồn tại.");
            }

            var doctor = new Doctors
            {
                DoctorId = Guid.NewGuid(),
                FullName = request.FullName,
                Specialty = request.Specialty,
                CurrentHospitalName = request.CurrentHospitalName,
                LicenseNumber = request.LicenseNumber,
                YearsOfExperience = request.YearsOfExperience,
                Bio = request.Bio,
                UserId = request.UserId,
                CreatedAt = DateTime.Now,
                Status = DoctorStatuses.Pending
            };

            await _repo.AddDoctorAsync(doctor);
            return MapToDto(doctor);
        }

        public async Task<DoctorDto> UpdateDoctorAsync(Guid doctorId, UpdateDoctorDto request)
        {
            var doctor = await _repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            }

            doctor.FullName = request.FullName;
            doctor.Specialty = request.Specialty;
            doctor.CurrentHospitalName = request.CurrentHospitalName;
            doctor.LicenseNumber = request.LicenseNumber;
            doctor.YearsOfExperience = request.YearsOfExperience;
            doctor.Bio = request.Bio;

            await _repo.UpdateDoctorAsync(doctor);
            return MapToDto(doctor);
        }

        public async Task<DoctorDto> ApproveDoctorAsync(Guid doctorId, ApproveDoctorDto request)
        {
            var doctor = await _repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            }

            var action = request.Action?.Trim().ToLowerInvariant();
            doctor.Status = action switch
            {
                "approve" => DoctorStatuses.Approved,
                "reject" => DoctorStatuses.Rejected,
                _ => throw new BadRequestException("Action phải là 'approve' hoặc 'reject'.")
            };

            await _repo.UpdateDoctorAsync(doctor);
            return MapToDto(doctor);
        }

        public async Task<DoctorAvailabilityDto> AddAvailabilityAsync(Guid doctorId, CreateDoctorAvailabilityDto request)
        {
            var doctor = await _repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            }

            var startTime = ParseTime(request.StartTime, "StartTime");
            var endTime = ParseTime(request.EndTime, "EndTime");
            ValidateRange(startTime, endTime);

            var availability = new DoctorAvailability
            {
                DoctorAvailabilityId = Guid.NewGuid(),
                DoctorId = doctorId,
                DayOfWeek = request.DayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                IsActive = true
            };

            await _repo.AddAvailabilityAsync(availability);
            return MapToDto(availability);
        }

        public async Task<DoctorAvailabilityDto> UpdateAvailabilityAsync(Guid doctorId, Guid availabilityId, UpdateDoctorAvailabilityDto request)
        {
            var availability = await _repo.GetAvailabilityByIdAsync(doctorId, availabilityId);
            if (availability == null)
            {
                throw new NotFoundException("Không tìm thấy lịch làm việc.");
            }

            var startTime = ParseTime(request.StartTime, "StartTime");
            var endTime = ParseTime(request.EndTime, "EndTime");
            ValidateRange(startTime, endTime);

            availability.DayOfWeek = request.DayOfWeek;
            availability.StartTime = startTime;
            availability.EndTime = endTime;
            availability.IsActive = request.IsActive;

            await _repo.UpdateAvailabilityAsync(availability);
            return MapToDto(availability);
        }

        public async Task DeleteAvailabilityAsync(Guid doctorId, Guid availabilityId)
        {
            var availability = await _repo.GetAvailabilityByIdAsync(doctorId, availabilityId);
            if (availability == null)
            {
                throw new NotFoundException("Không tìm thấy lịch làm việc.");
            }

            await _repo.DeleteAvailabilityAsync(availability);
        }

        private static DoctorDto MapToDto(Doctors e)
        {
            return new()
            {
                DoctorId = e.DoctorId,
                FullName = e.FullName,
                Specialty = e.Specialty,
                CurrentHospitalName = e.CurrentHospitalName,
                LicenseNumber = e.LicenseNumber,
                YearsOfExperience = e.YearsOfExperience,
                Bio = e.Bio,
                AverageRating = e.AverageRating,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                UserId = e.UserId
            };
        }

        private static DoctorAvailabilityDto MapToDto(DoctorAvailability e)
        {
            return new()
            {
                DoctorAvailabilityId = e.DoctorAvailabilityId,
                DoctorId = e.DoctorId,
                DayOfWeek = e.DayOfWeek,
                StartTime = $"{e.StartTime.Hours:D2}:{e.StartTime.Minutes:D2}",
                EndTime = $"{e.EndTime.Hours:D2}:{e.EndTime.Minutes:D2}",
                IsActive = e.IsActive
            };
        }

        private static DoctorAvailabilityExceptionDto MapToDto(DoctorAvailabilityExceptions e)
        {
            return new()
            {
                ExceptionId = e.ExceptionId,
                DoctorId = e.DoctorId,
                Date = e.Date,
                StartTime = e.StartTime.HasValue ? $"{e.StartTime.Value.Hours:D2}:{e.StartTime.Value.Minutes:D2}" : null,
                EndTime = e.EndTime.HasValue ? $"{e.EndTime.Value.Hours:D2}:{e.EndTime.Value.Minutes:D2}" : null,
                Reason = e.Reason,
                IsAvailableOverride = e.IsAvailableOverride
            };
        }

        private static TimeSpan ParseTime(string value, string fieldName)
        {
            if (!TimeSpan.TryParse(value, out var result))
            {
                throw new BadRequestException($"{fieldName} không đúng định dạng HH:mm.");
            }

            return result;
        }

        private static void ValidateRange(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
            {
                throw new BadRequestException("StartTime phải nhỏ hơn EndTime.");
            }
        }
    }
}
