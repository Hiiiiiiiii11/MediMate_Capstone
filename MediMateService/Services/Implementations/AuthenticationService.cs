using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Share.Common;
using Share.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MediMateService.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthenticationRepository _authRepo;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        private static readonly HashSet<string> RemainingRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "Doctor", "Staff", "DoctorManager"
        };

        public AuthenticationService(IUnitOfWork unitOfWork, IAuthenticationRepository authRepo, IConfiguration configuration, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _authRepo = authRepo;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<ApiResponse<AutheticationResponse>> RegisterAsync(RegisterRequest request)
        {
            // 1. Kiểm tra tồn tại (SĐT hoặc Email)
            // Giả sử bạn đã thêm hàm IsUserExistsAsync vào IAuthenticationRepository như bài trước
            // Nếu chưa, có thể check thủ công bằng 2 query
            var existingUser = await _authRepo.GetUserByEmailOrPhoneAsync(request.PhoneNumber);
            if (existingUser != null)
            {
                return ApiResponse<AutheticationResponse>.Fail("Số điện thoại này đã được sử dụng.", 409);
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingEmail = await _authRepo.GetUserByEmailOrPhoneAsync(request.Email);
                if (existingEmail != null)
                {
                    return ApiResponse<AutheticationResponse>.Fail("Email này đã được sử dụng.", 409);
                }
            }

            // 2. Hash mật khẩu
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Sinh OTP
            int otp = new Random().Next(100000, 999999);

            // 4. Tạo Entity
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = passwordHash,
                Role = "User",
                IsActive = false, // Vô hiệu hóa cho đến khi verify
                VerifyCode = otp,
                ExpiriedAt = DateTime.Now.AddMinutes(30),
                CreatedAt = DateTime.Now
            };

            // 5. Lưu vào DB
            await _authRepo.AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            // 6. Gửi Email OTP
            if (!string.IsNullOrEmpty(newUser.Email))
            {
                string subject = "Xác thực tài khoản MediMate+";
                string body = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>OTP Verification</title>
  <link href=""https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600&display=swap"" rel=""stylesheet"" />
</head>
<body style=""margin: 0; font-family: 'Poppins', sans-serif; background: #ffffff; font-size: 14px;"">
  <div style=""max-width: 680px; margin: 0 auto; padding: 45px 30px 60px; background: #f4f7ff; background-image: url(https://archisketch-resources.s3.ap-northeast-2.amazonaws.com/vrstyler/1661497957196_595865/email-template-background-banner); background-repeat: no-repeat; background-size: 800px 452px; background-position: top center; color: #434343;"">
    <main style=""margin: 0; margin-top: 70px; padding: 92px 30px 115px; background: #ffffff; border-radius: 30px; text-align: center;"">
      <div style=""width: 100%; max-width: 489px; margin: 0 auto;"">
        <h1 style=""margin: 0; font-size: 24px; font-weight: 500; color: #1f1f1f;"">Xác thực OTP</h1>
        <p style=""margin: 0; margin-top: 17px; font-size: 16px; font-weight: 500;"">Xin chào {newUser.FullName},</p>
        <p style=""margin: 0; margin-top: 17px; font-weight: 500; letter-spacing: 0.56px;"">
          Cảm ơn bạn đã lựa chọn MediMate+. Mã xác nhận để kích hoạt tài khoản của bạn là mã dưới đây, có hiệu lực trong <strong>30 phút</strong>. Vui lòng không chia sẻ mã này cho người khác.
        </p>
        <p style=""margin: 0; margin-top: 60px; font-size: 40px; font-weight: 600; letter-spacing: 12px; color: #ba3d4f;"">
          {otp}
        </p>
      </div>
    </main>
  </div>
</body>
</html>";

                await _emailService.SendEmailAsync(newUser.Email, subject, body);
            }

            return ApiResponse<AutheticationResponse>.Ok(new AutheticationResponse(), "Đăng ký thành công. Vui lòng kiểm tra email để nhận mã OTP xác thực tài khoản.");
        }

        public async Task<ApiResponse<AutheticationResponse>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var user = await _authRepo.GetUserByEmailOrPhoneAsync(request.Email);
            
            if (user == null)
            {
                return ApiResponse<AutheticationResponse>.Fail("Không tìm thấy người dùng này.", 404);
            }

            if (user.IsActive)
            {
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản đã được kích hoạt trước đó.", 400);
            }

            if (user.VerifyCode != request.VerifyCode)
            {
                return ApiResponse<AutheticationResponse>.Fail("Mã xác thực không chính xác.", 400);
            }

            if (user.ExpiriedAt.HasValue && user.ExpiriedAt.Value < DateTime.Now)
            {
                return ApiResponse<AutheticationResponse>.Fail("Mã xác thực đã hết hạn. Vui lòng yêu cầu mã mới.", 400);
            }

            // Thành công -> Kích hoạt tài khoản
            user.IsActive = true;
            user.VerifyCode = null;
            user.ExpiriedAt = null;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            // Sinh Token đăng nhập luôn
            var token = GenerateJwtToken(user, "user");
            var responseData = new AutheticationResponse
            {
                AccessToken = token
            };

            return ApiResponse<AutheticationResponse>.Ok(responseData, "Kích hoạt tài khoản thành công.");
        }

        public async Task<ApiResponse<string>> LoginDependentByQrAsync(DependentQrLoginRequest request)
        {
            // 1. Validate định dạng mã
            if (string.IsNullOrEmpty(request.QrData) || !request.QrData.StartsWith("LOGIN-"))
            {
                return ApiResponse<string>.Fail("Mã QR không hợp lệ.", 400);
            }

            // Tách lấy SyncToken thực tế
            var syncToken = request.QrData.Substring(6); // Bỏ chữ "LOGIN-"

            // 2. Tìm Member có SyncToken này
            var member = (await _unitOfWork.Repository<Members>()
                .FindAsync(m => m.SyncToken == syncToken)).FirstOrDefault();

            if (member == null)
            {
                return ApiResponse<string>.Fail("Mã đăng nhập không chính xác hoặc đã được sử dụng.", 401);
            }

            // 3. Kiểm tra mã hết hạn chưa
            if (member.SyncTokenExpireAt == null || member.SyncTokenExpireAt < DateTime.Now)
            {
                return ApiResponse<string>.Fail("Mã QR đăng nhập đã hết hạn. Vui lòng tạo mã mới.", 401);
            }

            // 4. THÀNH CÔNG: Xóa SyncToken để tránh dùng lại
            member.SyncToken = null;
            member.SyncTokenExpireAt = null;


            if (!string.IsNullOrEmpty(request.FcmToken))
            {
                member.FcmToken = request.FcmToken;
            }

            _unitOfWork.Repository<Members>().Update(member);
            await _unitOfWork.CompleteAsync();

            // 5. Sinh JWT Token đặc biệt cho Dependent
            // Hàm này tương tự hàm GenerateToken cho User của bạn, nhưng dùng MemberId thay vì UserId
            var token = GenerateJwtTokenForDependent(member, "dependent");

            return ApiResponse<string>.Ok(token, $"Đăng nhập thành công với tư cách: {member.FullName}");
        }


        public string GenerateJwtTokenForDependent(Members member, string typeLogin)
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
        // Quan trọng: Gắn NameIdentifier là MemberId
        new Claim("MemberId", member.MemberId.ToString()),
        new Claim("Name", member.FullName ?? "Dependent"),
        new Claim("Role", "Dependent"),
        new Claim("typeLogin", typeLogin)// Gắn Role Dependent để phân biệt nếu cần
             };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(300), // Dependent thường ít bị out, cho sống 30 ngày
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<ApiResponse<AutheticationResponse>> LoginUserAsync(LoginRequest request)
        {
            var user = await _authRepo.GetUserByEmailOrPhoneAsync(request.Identifier);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác.", 401);

            if (!user.IsActive)
            {
                if (user.VerifyCode != null)
                    return ApiResponse<AutheticationResponse>.Fail("Vui lòng kích hoạt tài khoản của bạn. Kiểm tra email để lấy mã OTP.", 403);
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản đã bị khóa hoặc vô hiệu hóa.", 403);
            }

            if (!string.Equals(user.Role, "User", StringComparison.OrdinalIgnoreCase))
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản không có quyền đăng nhập tại đây.", 403);
            if (!string.IsNullOrEmpty(request.FcmToken))
            {
                user.FcmToken = request.FcmToken;

                // Lưu vào DB (Tùy theo cấu trúc Repo của bạn, có thể dùng _authRepo hoặc _unitOfWork)
                _unitOfWork.Repository<User>().Update(user);
                await _unitOfWork.CompleteAsync();
            }
            var token = GenerateJwtToken(user, "user");
            return ApiResponse<AutheticationResponse>.Ok(new AutheticationResponse { AccessToken = token }, "Đăng nhập thành công.");
        }

        public async Task<ApiResponse<AutheticationResponse>> LoginRemainingAsync(LoginRequest request)
        {
            var user = await _authRepo.GetUserByEmailOrPhoneAsync(request.Identifier);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác.", 401);

            if (!user.IsActive && !string.Equals(user.Role, Roles.Doctor, StringComparison.OrdinalIgnoreCase))
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản đã bị khóa hoặc vô hiệu hóa.", 403);

            if (!RemainingRoles.Contains(user.Role ?? ""))
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản không có quyền đăng nhập tại đây.", 403);

            var typeLogin = string.Equals(user.Role, Roles.DoctorManager, StringComparison.OrdinalIgnoreCase) 
                ? "doctormanager" 
                : "admin";

            var token = GenerateJwtToken(user, typeLogin);
            return ApiResponse<AutheticationResponse>.Ok(new AutheticationResponse { AccessToken = token }, "Đăng nhập thành công.");
        }


        private string GenerateJwtToken(User user, string typeLogin)
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationHours = double.Parse(jwtSettings["ExpirationHours"] ?? "24");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("Id", user.UserId.ToString() ?? ""),
                new Claim("Role", user.Role ?? ""),
                new Claim("FullName", user.FullName ?? ""),
                new Claim("Phone", user.PhoneNumber),
                new Claim("typeLogin", typeLogin)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(expirationHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<ApiResponse<bool>> LogoutAsync(Guid accountId, string role)
        {
            if (role == "Dependent")
            {
                // Nếu là người phụ thuộc đăng xuất -> Xóa token ở bảng Members
                var member = await _unitOfWork.Repository<Members>().GetByIdAsync(accountId);
                if (member != null)
                {
                    member.FcmToken = null;
                    _unitOfWork.Repository<Members>().Update(member);
                }
            }
            else
            {
                // Nếu là User, Admin, Doctor... đăng xuất -> Xóa token ở bảng User
                var user = await _unitOfWork.Repository<User>().GetByIdAsync(accountId);
                if (user != null)
                {
                    user.FcmToken = null;
                    _unitOfWork.Repository<User>().Update(user);
                }
            }

            // Lưu thay đổi vào Database
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.Ok(true, "Đăng xuất thành công.");
        }

    }
}
