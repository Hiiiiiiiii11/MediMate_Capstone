using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MediMateService.DTOs
{
    // --- RESPONSE: Dữ liệu trả về cho Client ---
    public class UserProfileResponse
    {
        public Guid UserId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserBankInfo? BankAccount { get; set; }
    }

    /// <summary>Thông tin ngân hàng nhúng vào UserProfileResponse</summary>
    public class UserBankInfo
    {
        public Guid BankAccountId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateDoctorManagerDto
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    // --- REQUEST: Cập nhật thông tin ---
    public class UpdateProfileRequest
    {
        //[Required(ErrorMessage = "Họ tên không được để trống")]
        public string? FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    // --- REQUEST: Đổi mật khẩu ---
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
    public class DeleteAccountRequest
    {
        public string Password { get; set; } // Yêu cầu nhập mật khẩu để xác nhận xóa
    }
}