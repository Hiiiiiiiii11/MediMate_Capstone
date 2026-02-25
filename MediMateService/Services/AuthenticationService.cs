using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Share.Common;
using Share.Constants;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Services
{
    public interface IAuthenticationService
    {
        Task<ApiResponse<AutheticationResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AutheticationResponse>> LoginAsync(LoginRequest request);
    }


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
