using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;

namespace MediMateService.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IMockDoctorRepository _repo;

        public DoctorService(IMockDoctorRepository repo)
        {
            _repo = repo;
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
            if (doc == null)
                throw new NotFoundException("Không tìm thấy bác sĩ.");
            return MapToDto(doc);
        }

        public async Task<List<DoctorAvailabilityDto>> GetAvailabilityByDoctorAsync(Guid doctorId)
        {
            var doc = await _repo.GetDoctorByIdAsync(doctorId);
            if (doc == null)
                throw new NotFoundException("Không tìm thấy bác sĩ.");

            var list = await _repo.GetAvailabilityByDoctorIdAsync(doctorId);
            var dtos = list.Select(MapToDto).ToList();
            return dtos;
        }

        public async Task<List<DoctorAvailabilityExceptionDto>> GetExceptionsByDoctorAsync(Guid doctorId)
        {
            var doc = await _repo.GetDoctorByIdAsync(doctorId);
            if (doc == null)
                throw new NotFoundException("Không tìm thấy bác sĩ.");

            var list = await _repo.GetExceptionsByDoctorIdAsync(doctorId);
            var dtos = list.Select(MapToDto).ToList();
            return dtos;
        }

        private static DoctorDto MapToDto(Doctors e) => new()
        {
            DoctorId = e.DoctorId,
            FullName = e.FullName,
            Specialty = e.Specialty,
            CurrentHospitalName = e.CurrentHospitalName,
            LicenseNumber = e.LicenseNumber,
            YearsOfExperience = e.YearsOfExperience,
            Bio = e.Bio,
            AverageRating = e.AverageRating,
            IsVerified = e.IsVerified,
            CreatedAt = e.CreatedAt,
            UserId = e.UserId
        };

        private static DoctorAvailabilityDto MapToDto(DoctorAvailability e) => new()
        {
            DoctorAvailabilityId = e.DoctorAvailabilityId,
            DoctorId = e.DoctorId,
            DayOfWeek = e.DayOfWeek,
            StartTime = $"{e.StartTime.Hours:D2}:{e.StartTime.Minutes:D2}",
            EndTime = $"{e.EndTime.Hours:D2}:{e.EndTime.Minutes:D2}",
            IsBooked = e.IsBooked
        };

        private static DoctorAvailabilityExceptionDto MapToDto(DoctorAvailabilityExceptions e) => new()
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
}
