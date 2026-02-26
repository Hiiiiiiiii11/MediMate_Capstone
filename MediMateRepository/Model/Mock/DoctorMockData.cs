using System;
using System.Collections.Generic;

namespace MediMateRepository.Model.Mock
{
    public static class DoctorMockData
    {
        public static readonly List<Doctors> Doctors = new()
        {
            new()
            {
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                FullName = "BS Nguyễn Văn An",
                Specialty = "Nội khoa",
                CurrentHospitalName = "Bệnh viện Bạch Mai",
                LicenseNumber = "BYT-12345",
                YearsOfExperience = 12,
                Bio = "Chuyên gia nội khoa, nhiều năm kinh nghiệm khám và điều trị bệnh mãn tính.",
                AverageRating = 4.8,
                IsVerified = true,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000001")
            },
            new()
            {
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                FullName = "BS Trần Thị Bình",
                Specialty = "Nhi khoa",
                CurrentHospitalName = "Bệnh viện Nhi Trung ương",
                LicenseNumber = "BYT-12346",
                YearsOfExperience = 8,
                Bio = "Bác sĩ nhi khoa, chuyên khám và tư vấn dinh dưỡng trẻ em.",
                AverageRating = 4.9,
                IsVerified = true,
                IsActive = true,
                CreatedAt = new DateTime(2024, 2, 20, 0, 0, 0, DateTimeKind.Utc),
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000002")
            },
            new()
            {
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                FullName = "BS Lê Minh Cường",
                Specialty = "Tim mạch",
                CurrentHospitalName = "Bệnh viện Việt Đức",
                LicenseNumber = "BYT-12347",
                YearsOfExperience = 15,
                Bio = "Chuyên gia tim mạch, tư vấn và điều trị bệnh huyết áp, tim mạch.",
                AverageRating = 4.7,
                IsVerified = true,
                IsActive = true,
                CreatedAt = new DateTime(2024, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000003")
            }
        };

        public static readonly List<DoctorAvailability> Availability = new()
        {
            new()
            {
                DoctorAvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222201"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                DayOfWeek = "Monday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                IsActive = true
            },
            new()
            {
                DoctorAvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222202"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                DayOfWeek = "Monday",
                StartTime = new TimeSpan(13, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsActive = true
            },
            new()
            {
                DoctorAvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222203"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                DayOfWeek = "Wednesday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                IsActive = true
            },
            new()
            {
                DoctorAvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222204"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                DayOfWeek = "Tuesday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsActive = true
            },
            new()
            {
                DoctorAvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222205"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                DayOfWeek = "Thursday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsActive = true
            },
            new()
            {
                DoctorAvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222206"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                DayOfWeek = "Monday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsActive = true
            },
            new()
            {
                DoctorAvailabilityId = Guid.Parse("22222222-2222-2222-2222-222222222207"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                DayOfWeek = "Friday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsActive = true
            }
        };

        public static readonly List<DoctorAvailabilityExceptions> Exceptions = new()
        {
            new()
            {
                ExceptionId = Guid.Parse("33333333-3333-3333-3333-333333333301"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                Date = new DateTime(2026, 2, 15),
                StartTime = null,
                EndTime = null,
                Reason = "Nghỉ phép",
                IsAvailableOverride = false
            },
            new()
            {
                ExceptionId = Guid.Parse("33333333-3333-3333-3333-333333333302"),
                DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                Date = new DateTime(2026, 2, 12),
                StartTime = new TimeSpan(13, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                Reason = "Họp nội bộ",
                IsAvailableOverride = false
            }
        };
    }
}

