using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Microsoft.Extensions.Configuration;
using Share.Constants;

namespace MediMateService.Services.Implementations
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public DoctorService(
            IDoctorRepository repo, 
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IConfiguration config)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _config = config;
        }

        public async Task<List<DoctorDto>> GetPublicDoctorsAsync(string? specialty = null)
        {
            var list = await _repo.GetPublicDoctorsAsync();
            if (!string.IsNullOrWhiteSpace(specialty))
                list = list.Where(d => d.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).ToList();
            return list.Select(MapToDto).ToList();
        }

        public async Task<DoctorDto> GetPublicDoctorByIdAsync(Guid doctorId)
        {
            var doc = await _repo.GetPublicDoctorByIdAsync(doctorId);
            return doc == null ? throw new NotFoundException("Không tìm thấy bác sĩ.") : MapToDto(doc);
        }

        //public async Task<List<DoctorAvailabilityDto>> GetPublicAvailabilityByDoctorAsync(Guid doctorId)
        //{
        //    var doc = await _repo.GetPublicDoctorByIdAsync(doctorId);
        //    if (doc == null) throw new NotFoundException("Không tìm thấy bác sĩ.");
        //    var list = (await _repo.GetAvailabilityByDoctorIdAsync(doctorId)).Where(a => a.IsActive).ToList();
        //    return list.Select(MapToDto).ToList();
        //}

        public async Task<List<DoctorDto>> GetDoctorsAsync(string? specialty = null, string? status = null)
        {
            var list = await _repo.GetAllDoctorsAsync();
            if (!string.IsNullOrWhiteSpace(specialty))
                list = list.Where(d => d.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(status))
                list = list.Where(d => d.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            return list.Select(MapToDto).ToList();
        }

        public async Task<DoctorDto> GetDoctorByIdAsync(Guid doctorId)
        {
            var doc = await _repo.GetDoctorByIdAsync(doctorId);
            return doc == null ? throw new NotFoundException("Không tìm thấy bác sĩ.") : MapToDto(doc);
        }

        public async Task<List<DoctorAvailabilityDto>> GetAvailabilityByDoctorAsync(Guid doctorId)
        {
            var doc = await _repo.GetDoctorByIdAsync(doctorId);
            if (doc == null) throw new NotFoundException("Không tìm thấy bác sĩ.");
            var list = await _repo.GetAvailabilityByDoctorIdAsync(doctorId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<DoctorAvailabilityExceptionDto>> GetExceptionsByDoctorAsync(Guid doctorId)
        {
            var doc = await _repo.GetDoctorByIdAsync(doctorId);
            if (doc == null) throw new NotFoundException("Không tìm thấy bác sĩ.");
            var list = await _repo.GetExceptionsByDoctorIdAsync(doctorId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var mailExists = (await userRepo.GetAllAsync())
                .Any(u => u.Email == request.Email);
            var phoneExists = (await userRepo.GetAllAsync())
                .Any(u => u.PhoneNumber == request.PhoneNumber);
            if (mailExists)
            {
                throw new ConflictException("Email đã tồn tại.", ErrorCodes.EmailExists);
            }
            if (phoneExists)
            {
                throw new ConflictException("Số điện thoại đã tồn tại.", ErrorCodes.PhoneExists);
            }
            var newUserId = Guid.NewGuid();
            var newUser = new User
            {
                UserId = newUserId,
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                Role = Roles.Doctor,
                IsActive = false,
                CreatedAt = DateTime.Now,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678aA@") // Mật khẩu mặc định hoặc bắt buộc họ đổi sau
            };

            await userRepo.AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            var doctor = new Doctors
            {
                DoctorId = Guid.NewGuid(),
                FullName = request.FullName,
                UserId = newUserId,
                CreatedAt = DateTime.Now,
                Status = DoctorStatuses.Inactive
            };

            await _repo.AddDoctorAsync(doctor);

            if (!string.IsNullOrEmpty(request.Email))
            {
                string subject = "Tài khoản Bác sĩ MediMate+ của bạn đã được tạo";
                string body = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Tài khoản Bác sĩ</title>
</head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f7ff; color: #333; padding: 20px;"">
    <div style=""max-width: 600px; margin: 0 auto; background: #fff; padding: 30px; border-radius: 8px;"">
        <h2 style=""color: #2c3e50;"">Xin chào Bác sĩ {request.FullName},</h2>
        <p>Quản trị viên MediMate+ đã tạo tài khoản Bác sĩ cho bạn.</p>
        <p>Thông tin đăng nhập của bạn:</p>
        <ul>
            <li><strong>Email:</strong> {request.Email}</li>
            <li><strong>Mật khẩu mặc định:</strong> 12345678aA@</li>
        </ul>
        <p>Vui lòng đăng nhập vào ứng dụng và <strong>đổi mật khẩu ngay lập tức</strong> để bảo mật tài khoản, sau đó <strong>bổ sung hồ sơ hành nghề</strong> để được xét duyệt.</p>
        <p>Trân trọng,<br/>Đội ngũ MediMate+</p>
    </div>
</body>
</html>";
                await _emailService.SendEmailAsync(request.Email, subject, body);
            }

            return MapToDto(doctor);
        }

        public async Task<DoctorDto> GetMyProfileAsync(Guid userId)
        {
            var doctors = await _repo.GetAllDoctorsAsync();
            var doctor = doctors.FirstOrDefault(d => d.UserId == userId);
            if (doctor == null) throw new NotFoundException("Không tìm thấy hồ sơ bác sĩ.");

            return MapToDto(doctor);
        }

        public async Task<DoctorDto> UpdateMyProfileAsync(Guid userId, UpdateDoctorDto request)
        {
            var doctors = await _repo.GetAllDoctorsAsync();
            var doctor = doctors.FirstOrDefault(d => d.UserId == userId);
            if (doctor == null) throw new NotFoundException("Không tìm thấy hồ sơ bác sĩ.");

            bool isUpdated = false;

            if (!string.IsNullOrWhiteSpace(request.FullName)) { doctor.FullName = request.FullName.Trim(); isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(request.Specialty)) { doctor.Specialty = request.Specialty.Trim(); isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(request.CurrentHospitalName)) { doctor.CurrentHospitalName = request.CurrentHospitalName.Trim(); isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(request.LicenseNumber)) { doctor.LicenseNumber = request.LicenseNumber.Trim(); isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(request.LicenseImage)) { doctor.LicenseImage = request.LicenseImage.Trim(); isUpdated = true; }
            if (request.YearsOfExperience.HasValue) { doctor.YearsOfExperience = request.YearsOfExperience.Value; isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(request.Bio)) { doctor.Bio = request.Bio.Trim(); isUpdated = true; }
            
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                var userRepo = _unitOfWork.Repository<User>();
                var user = await userRepo.GetByIdAsync(doctor.UserId);
                if (user != null)
                {
                    user.AvatarUrl = request.AvatarUrl.Trim();
                    userRepo.Update(user);
                    isUpdated = true;
                }
            }

            if (isUpdated)
            {
                if (doctor.Status != DoctorStatuses.Inactive && doctor.Status != DoctorStatuses.Pending)
                {
                    doctor.Status = DoctorStatuses.Pending;
                }
                await _repo.UpdateDoctorAsync(doctor);
                await _unitOfWork.CompleteAsync();
            }

            return MapToDto(doctor);
        }

        public async Task<DoctorDto> SubmitPendingAsync(Guid doctorId, SubmitDoctorDto dto)
        {
            var doctor = await _repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null) throw new NotFoundException("Không tìm thấy bác sĩ.");
            if (doctor.Status != DoctorStatuses.Inactive)
                throw new BadRequestException($"Chỉ có thể submit khi trạng thái là Inactive. Hiện tại: {doctor.Status}");

            doctor.FullName = dto.FullName;
            doctor.Specialty = dto.Specialty;
            doctor.CurrentHospitalName = dto.CurrentHospitalName;
            doctor.LicenseNumber = dto.LicenseNumber;
            doctor.LicenseImage = dto.LicenseImage;
            doctor.YearsOfExperience = dto.YearsOfExperience;
            doctor.Bio = dto.Bio;

            if (!string.IsNullOrWhiteSpace(dto.AvatarUrl) && doctor.User != null)
            {
                doctor.User.AvatarUrl = dto.AvatarUrl.Trim();
            }

            doctor.Status = DoctorStatuses.Pending;
            doctor.RejectionReason = null;

            await _repo.UpdateDoctorAsync(doctor);
            return MapToDto(doctor);
        }

        public async Task<DoctorDto> VerifyDoctorAsync(Guid doctorId)
        {
            var doctor = await _repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null) throw new NotFoundException("Không tìm thấy bác sĩ.");
            if (doctor.Status != DoctorStatuses.Pending)
                throw new BadRequestException($"Chỉ có thể verify khi trạng thái là Pending. Hiện tại: {doctor.Status}");

            doctor.Status = DoctorStatuses.Verified;
            await _repo.UpdateDoctorAsync(doctor);

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(doctor.UserId);
            if (user != null)
            {
                var otp = new Random().Next(100000, 999999);
                user.VerifyCode = otp;
                user.ExpiriedAt = DateTime.Now.AddMinutes(30);
                userRepo.Update(user);
                await _unitOfWork.CompleteAsync();

                if (!string.IsNullOrEmpty(user.Email))
                {
                    string subject = "Tài khoản Bác sĩ của bạn đã được duyệt";
                    string body = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <meta http-equiv=""X-UA-Compatible"" content=""ie=edge"" />
  <title>OTP Verification</title>
  <link href=""https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600&display=swap"" rel=""stylesheet"" />
</head>
<body style=""margin: 0; font-family: 'Poppins', sans-serif; background: #ffffff; font-size: 14px;"">
  <div style=""max-width: 680px; margin: 0 auto; padding: 45px 30px 60px; background: #f4f7ff; background-image: url(https://archisketch-resources.s3.ap-northeast-2.amazonaws.com/vrstyler/1661497957196_595865/email-template-background-banner); background-repeat: no-repeat; background-size: 800px 452px; background-position: top center; color: #434343;"">
    <main style=""margin: 0; margin-top: 70px; padding: 92px 30px 115px; background: #ffffff; border-radius: 30px; text-align: center;"">
      <div style=""width: 100%; max-width: 489px; margin: 0 auto;"">
        <h1 style=""margin: 0; font-size: 24px; font-weight: 500; color: #1f1f1f;"">Your OTP</h1>
        <p style=""margin: 0; margin-top: 17px; font-size: 16px; font-weight: 500;"">Hey {user.FullName},</p>
        <p style=""margin: 0; margin-top: 17px; font-weight: 500; letter-spacing: 0.56px;"">
          Thank you for choosing MediMate+. Use the following OTP to complete the activation procedure for your doctor account. OTP is valid for <strong>30 minutes</strong>. Do not share this code with others.
        </p>
        <p style=""margin: 0; margin-top: 60px; font-size: 40px; font-weight: 600; letter-spacing: 12px; color: #ba3d4f;"">
          {otp}
        </p>
      </div>
    </main>
  </div>
</body>
</html>";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                }
            }

            return MapToDto(doctor);
        }

        public async Task<DoctorDto> ActivateDoctorAsync(Guid doctorId, int verifyCode)
        {
            var doctor = await _repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null) throw new NotFoundException("Không tìm thấy bác sĩ.");
            if (doctor.Status != DoctorStatuses.Verified)
                throw new BadRequestException($"Chỉ có thể activate khi trạng thái là Verified. Hiện tại: {doctor.Status}");

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(doctor.UserId);
            if (user == null) throw new NotFoundException("Không tìm thấy tài khoản User liên kết.");

            if (user.VerifyCode != verifyCode)
                throw new BadRequestException("Mã xác thực không chính xác.");

            if (user.ExpiriedAt.HasValue && user.ExpiriedAt.Value < DateTime.Now)
                throw new BadRequestException("Mã xác thực đã hết hạn.");

            user.VerifyCode = null;
            user.ExpiriedAt = null;

            doctor.Status = DoctorStatuses.Active;
            await _repo.UpdateDoctorAsync(doctor);

            user.IsActive = true;
            userRepo.Update(user);
            await _unitOfWork.CompleteAsync();

            return MapToDto(doctor);
        }

        public async Task<DoctorDto> RejectDoctorAsync(Guid doctorId, string? reason)
        {
            var doctor = await _repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null) throw new NotFoundException("Không tìm thấy bác sĩ.");
            if (doctor.Status == DoctorStatuses.Rejected)
                throw new BadRequestException("Bác sĩ đã bị từ chối trước đó.");

            doctor.Status = DoctorStatuses.Rejected;
            doctor.RejectionReason = reason;
            await _repo.UpdateDoctorAsync(doctor);

            await SyncUserIsActiveAsync(doctor.UserId, isActive: false);

            return MapToDto(doctor);
        }

        public async Task HeartbeatAsync(Guid doctorId)
        {
            var doctor = await _repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null) throw new NotFoundException("Không tìm thấy bác sĩ.");

            doctor.LastSeenAt = DateTime.Now;
            await _repo.UpdateDoctorAsync(doctor);

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(doctor.UserId);
            if (user != null)
            {
                user.LastSeenAt = DateTime.Now;
                userRepo.Update(user);
                await _unitOfWork.CompleteAsync();
            }
        }

        //public async Task<DoctorAvailabilityDto> AddAvailabilityAsync(Guid doctorId, CreateDoctorAvailabilityDto request)
        //{
        //    var doctor = await _repo.GetDoctorByIdAsync(doctorId);
        //    if (doctor == null) throw new NotFoundException("Không tìm thấy bác sĩ.");

        //    var startTime = ParseTime(request.StartTime, "StartTime");
        //    var endTime = ParseTime(request.EndTime, "EndTime");
        //    ValidateRange(startTime, endTime);

        //    var availability = new DoctorAvailability
        //    {
        //        DoctorAvailabilityId = Guid.NewGuid(),
        //        DoctorId = doctorId,
        //        DayOfWeek = request.DayOfWeek,
        //        StartTime = startTime,
        //        EndTime = endTime,
        //        IsActive = true
        //    };

        //    await _repo.AddAvailabilityAsync(availability);
        //    return MapToDto(availability);
        //}

        //public async Task<DoctorAvailabilityDto> UpdateAvailabilityAsync(Guid doctorId, Guid availabilityId, UpdateDoctorAvailabilityDto request)
        //{
        //    var availability = await _repo.GetAvailabilityByIdAsync(doctorId, availabilityId);
        //    if (availability == null) throw new NotFoundException("Không tìm thấy lịch làm việc.");

        //    var startTime = ParseTime(request.StartTime, "StartTime");
        //    var endTime = ParseTime(request.EndTime, "EndTime");
        //    ValidateRange(startTime, endTime);

        //    availability.DayOfWeek = request.DayOfWeek;
        //    availability.StartTime = startTime;
        //    availability.EndTime = endTime;
        //    availability.IsActive = request.IsActive;

        //    await _repo.UpdateAvailabilityAsync(availability);
        //    return MapToDto(availability);
        //}

        //public async Task DeleteAvailabilityAsync(Guid doctorId, Guid availabilityId)
        //{
        //    var availability = await _repo.GetAvailabilityByIdAsync(doctorId, availabilityId);
        //    if (availability == null) throw new NotFoundException("Không tìm thấy lịch làm việc.");
        //    await _repo.DeleteAvailabilityAsync(availability);
        //}

        private async Task SyncUserIsActiveAsync(Guid userId, bool isActive)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = isActive;
                userRepo.Update(user);
                await _unitOfWork.CompleteAsync();
            }
        }

        private static DoctorDto MapToDto(Doctors e) => new()
        {
            DoctorId = e.DoctorId,
            FullName = e.FullName,
            Specialty = e.Specialty,
            CurrentHospitalName = e.CurrentHospitalName,
            LicenseNumber = e.LicenseNumber,
            LicenseImage = e.LicenseImage,
            YearsOfExperience = e.YearsOfExperience,
            Bio = e.Bio,
            Status = e.Status,
            RejectionReason = e.RejectionReason,
            LastSeenAt = e.LastSeenAt,
            CreatedAt = e.CreatedAt,
            UserId = e.UserId,
            AvatarUrl = e.User?.AvatarUrl
        };

        private static DoctorAvailabilityDto MapToDto(DoctorAvailability e) => new()
        {
            DoctorAvailabilityId = e.DoctorAvailabilityId,
            DoctorId = e.DoctorId,
            DayOfWeek = e.DayOfWeek,
            StartTime = $"{e.StartTime.Hours:D2}:{e.StartTime.Minutes:D2}",
            EndTime = $"{e.EndTime.Hours:D2}:{e.EndTime.Minutes:D2}",
            IsActive = e.IsActive
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

        private static TimeSpan ParseTime(string value, string fieldName)
        {
            if (!TimeSpan.TryParse(value, out var result))
                throw new BadRequestException($"{fieldName} không đúng định dạng HH:mm.");
            return result;
        }

        private static void ValidateRange(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
                throw new BadRequestException("StartTime phải nhỏ hơn EndTime.");
        }
    }
}
