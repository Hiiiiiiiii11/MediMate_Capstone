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
        private readonly IAuthenticationRepository _authRepo; // Inject riêng Repo này để dùng hàm custom
        private readonly IConfiguration _configuration;

        public AuthenticationService(IUnitOfWork unitOfWork, IAuthenticationRepository authRepo, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _authRepo = authRepo;
            _configuration = configuration;
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

            // 3. Tạo Entity
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = passwordHash,
                Role = Roles.User,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            // 4. Lưu vào DB
            await _authRepo.AddAsync(newUser);
            await _unitOfWork.CompleteAsync(); // Commit transaction

            // 5. Tạo Token để user login luôn sau khi đăng ký
            var token = GenerateJwtToken(newUser);

            // 6. Trả về Response
            var responseData = new AutheticationResponse
            {
                AccessToken = token
            };

            return ApiResponse<AutheticationResponse>.Ok(responseData, "Đăng ký tài khoản thành công.");
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
            if (member.SyncTokenExpireAt == null || member.SyncTokenExpireAt < DateTime.UtcNow)
            {
                return ApiResponse<string>.Fail("Mã QR đăng nhập đã hết hạn. Vui lòng tạo mã mới.", 401);
            }

            // 4. THÀNH CÔNG: Xóa SyncToken để tránh dùng lại
            member.SyncToken = null;
            member.SyncTokenExpireAt = null;
            _unitOfWork.Repository<Members>().Update(member);
            await _unitOfWork.CompleteAsync();

            // 5. Sinh JWT Token đặc biệt cho Dependent
            // Hàm này tương tự hàm GenerateToken cho User của bạn, nhưng dùng MemberId thay vì UserId
            var token = GenerateJwtTokenForDependent(member);

            return ApiResponse<string>.Ok(token, $"Đăng nhập thành công với tư cách: {member.FullName}");
        }


        private string GenerateJwtTokenForDependent(Members member)
        {
            var secretKey = _configuration["Jwt:Key"]; // Đọc từ appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        // Quan trọng: Gắn NameIdentifier là MemberId
        new Claim(ClaimTypes.NameIdentifier, member.MemberId.ToString()),
        new Claim(ClaimTypes.Name, member.FullName ?? "Dependent"),
        new Claim(ClaimTypes.Role, "Dependent") // Gắn Role Dependent để phân biệt nếu cần
    };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30), // Dependent thường ít bị out, cho sống 30 ngày
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<ApiResponse<AutheticationResponse>> LoginAsync(LoginRequest request)
        {
            // 1. Tìm user theo Email HOẶC Phone
            var user = await _authRepo.GetUserByEmailOrPhoneAsync(request.Identifier);
            if (!user.IsActive)
            {
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản của bạn đã bị khóa hoặc vô hiệu hóa.", 403);
            }
            // 2. Kiểm tra user có tồn tại không
            if (user == null)
            {
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác.", 401);
            }

            // 3. Kiểm tra mật khẩu (So sánh hash)
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản hoặc mật khẩu không chính xác.", 401);
            }

            // 4. Kiểm tra trạng thái hoạt động
            if (!user.IsActive)
            {
                return ApiResponse<AutheticationResponse>.Fail("Tài khoản của bạn đã bị khóa.", 403);
            }

            // 5. Tạo JWT Token
            var token = GenerateJwtToken(user);

            // 6. Trả về kết quả
            var responseData = new AutheticationResponse
            {

                AccessToken = token
            };

            return ApiResponse<AutheticationResponse>.Ok(responseData, "Đăng nhập thành công.");
        }

        // --- HELPER: GENERATE JWT ---
        private string GenerateJwtToken(User user)
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
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? Roles.User),
                new Claim("FullName", user.FullName ?? ""),
                new Claim("Phone", user.PhoneNumber)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
