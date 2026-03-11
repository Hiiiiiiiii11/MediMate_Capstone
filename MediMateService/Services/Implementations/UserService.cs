using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using MediMateService.Shared;
using Share.Common;
namespace MediMateService.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadPhotoService _uploadPhotoService;
        private readonly IEmailService _emailService;

        public UserService(IUnitOfWork unitOfWork, IUploadPhotoService uploadPhotoService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _uploadPhotoService = uploadPhotoService;
            _emailService = emailService;
        }
        public async Task<ApiResponse<IEnumerable<UserProfileResponse>>> GetAllUsersAsync()
        {
            // 1. Lấy toàn bộ user từ DB
            // Sử dụng GetAllAsync từ GenericRepository
            var users = await _unitOfWork.Repository<User>().GetAllAsync();

            // 2. Map từ Entity sang DTO
            // Lưu ý: Nếu data lớn, nên dùng AutoMapper hoặc Select trực tiếp từ IQueryable (tùy GenericRepo hỗ trợ)
            var userDtos = users.Select(u => new UserProfileResponse
            {
                UserId = u.UserId,
                PhoneNumber = u.PhoneNumber,
                FullName = u.FullName,
                Email = u.Email,
                DateOfBirth = u.DateOfBirth,
                Gender = u.Gender,
                AvatarUrl = u.AvatarUrl,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();

            // 3. Trả về kết quả
            return ApiResponse<IEnumerable<UserProfileResponse>>.Ok(userDtos, "Lấy danh sách người dùng thành công.");
        }

        public async Task<ApiResponse<UserProfileResponse>> CreateDoctorManagerAsync(CreateDoctorManagerDto request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var exists = (await userRepo.GetAllAsync())
                .Any(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber);
            if (exists) throw new ConflictException("Email hoặc số điện thoại đã tồn tại.");

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Role = Share.Constants.Roles.DoctorManager,
                IsActive = true,
                CreatedAt = DateTime.Now,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678aA@")
            };

            await userRepo.AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            var dto = new UserProfileResponse
            {
                UserId = newUser.UserId,
                PhoneNumber = newUser.PhoneNumber,
                FullName = newUser.FullName,
                Email = newUser.Email,
                Role = newUser.Role,
                IsActive = newUser.IsActive,
                CreatedAt = newUser.CreatedAt
            };

            return ApiResponse<UserProfileResponse>.Ok(dto, "Tạo tài khoản Doctor Manager thành công.");
        }
        public async Task<ApiResponse<UserProfileResponse>> GetProfileAsync(Guid callerId)
        {
            UserProfileResponse response = null;

            // --- TRƯỜNG HỢP 1: LÀ USER (BỐ/MẸ) ---
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(callerId);
            if (user != null)
            {
                response = new UserProfileResponse
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth, // Có thể null trong bảng User
                    Gender = user.Gender,
                    AvatarUrl = user.AvatarUrl, // Lấy avatar của User gốc
                    Role = user.Role ?? "User",
                    IsActive = user.IsActive,
                };
            }
            // --- TRƯỜNG HỢP 2: LÀ DEPENDENT (CON CÁI/MEMBER) ---
            else
            {
                var member = await _unitOfWork.Repository<Members>().GetByIdAsync(callerId);
                if (member != null)
                {
                    response = new UserProfileResponse
                    {
                        UserId = member.MemberId, // Trả về MemberId vào trường UserId để FE dùng thống nhất
                        FullName = member.FullName,
                        Email = null, // Member phụ thuộc thường không có email riêng
                        PhoneNumber = null,
                        DateOfBirth = member.DateOfBirth,
                        Gender = member.Gender,
                        AvatarUrl = member.AvatarUrl,
                        Role = member.Role ?? "Member", // Thường là "Member" hoặc "Dependent"
                        IsActive = member.IsActive,

                    };
                }
            }

            if (response == null)
            {
                return ApiResponse<UserProfileResponse>.Fail("Không tìm thấy hồ sơ người dùng.", 404);
            }

            return ApiResponse<UserProfileResponse>.Ok(response);
        }


        //chua co logic upload anh
        public async Task<ApiResponse<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            // 1. Lấy User
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserProfileResponse>.Fail("Không tìm thấy người dùng.", 404);
            }

            // 2. Cập nhật dữ liệu
            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }
            if (!string.IsNullOrEmpty(request.Email))
            {
                user.Email = request.Email;
            }
            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth.Value;
            }

            if (!string.IsNullOrEmpty(request.Gender))
            {
                user.Gender = request.Gender;
            }


            // Chỉ cập nhật Avatar nếu có dữ liệu mới (Nếu null thì giữ nguyên hoặc xử lý theo logic riêng)
            if (request.AvatarFile != null)
            {
                // Gọi hàm upload mới
                var uploadResult = await _uploadPhotoService.UploadPhotoAsync(request.AvatarFile);

                // Lưu link vào DB
                // Với Avatar, ta thường dùng OriginalUrl (đã được crop face ở service)
                user.AvatarUrl = uploadResult.OriginalUrl;
            }

            // 3. Lưu vào DB
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            // 4. Trả về data mới
            return await GetProfileAsync(userId);
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            // 1. Lấy User
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy người dùng.", 404);
            }

            // 2. Kiểm tra mật khẩu cũ
            bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);
            if (!isOldPasswordCorrect)
            {
                return ApiResponse<bool>.Fail("Mật khẩu cũ không chính xác.", 400);
            }

            // 3. Hash mật khẩu mới và lưu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đổi mật khẩu thành công.");
        }
        public async Task<ApiResponse<bool>> DeactivateUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("User not found", 404);
            }

            // Chuyển trạng thái sang false
            user.IsActive = false;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Tài khoản đã bị vô hiệu hóa.");
        }
        public async Task<ApiResponse<bool>> ActivateUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("User not found", 404);
            }

            // Chuyển trạng thái sang true
            user.IsActive = true;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Tài khoản đã được active");
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(Guid userId, DeleteAccountRequest request)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("User not found", 404);
            }

            // 1. Xác thực mật khẩu trước khi xóa
            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordCorrect)
            {
                return ApiResponse<bool>.Fail("Mật khẩu không chính xác. Không thể xóa tài khoản.", 400);
            }

            // 2. Xử lý dữ liệu liên quan (Quan trọng!)
            // User này đang sở hữu các hồ sơ Member trong các Family.
            // Nếu xóa User, các hồ sơ Member đó sẽ bị lỗi Foreign Key hoặc mất chủ.
            // GIẢI PHÁP: Giữ lại Member profile nhưng set UserId = null (thành hồ sơ mồ côi)

            var linkedMembers = await _unitOfWork.Repository<Members>().FindAsync(m => m.UserId == userId);
            foreach (var member in linkedMembers)
            {
                member.UserId = null; // Ngắt kết nối
                member.IsActive = false; // Tạm thời unactive profile đó (tùy nghiệp vụ)
                _unitOfWork.Repository<Members>().Update(member);
            }

            // 3. Xóa User
            _unitOfWork.Repository<User>().Remove(user);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Tài khoản đã được xóa vĩnh viễn.");
        }

    }
}
