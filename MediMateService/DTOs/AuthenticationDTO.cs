using System.ComponentModel.DataAnnotations;

namespace MediMateService.DTOs
{
    // --- REQUEST MODELS ---

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; } // Optional
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại")]
        public string Identifier { get; set; } = string.Empty; // Chấp nhận cả Email hoặc Phone

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;
        public string? FcmToken { get; set; }
    }

    // --- RESPONSE MODELS ---

    public class AutheticationResponse
    {
        //public Guid UserId { get; set; }
        //public string FullName { get; set; } = string.Empty;
        //public string? PhoneNumber { get; set; }
        //public string? Email { get; set; }
        //public string Role { get; set; } = Roles.User;
        //public string? AvatarUrl { get; set; }
        public string AccessToken { get; set; } = string.Empty;
    }

    public class DependentQrLoginRequest
    {
        public string QrData { get; set; } // Dữ liệu quét được từ QR (VD: "LOGIN-abc123xyz...")
        public string? FcmToken { get; set; }
    }
}