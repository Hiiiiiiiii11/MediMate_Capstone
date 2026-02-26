using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{
    public class InitDependentRequest
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = "Other";

        // TH 1: Bố mẹ tạo (Đã login) -> Truyền ID gia đình muốn thêm vào
        public Guid? TargetFamilyId { get; set; }

        // TH 2: Người phụ thuộc tự tạo (Chưa login) -> Truyền mã JoinCode
        public string? JoinCode { get; set; }
    }

    public class InitDependentResponse
    {
        public Guid MemberId { get; set; }
        public Guid FamilyId { get; set; }
        public string FullName { get; set; }
        public string FamilyName { get; set; }
        public string AccessToken { get; set; }

        // Đã xóa QrCodeUrl và IdentityCode vì không còn cần thiết
    }
    //request tạo lại qr
    public class MemberQrResponse
    {
        public Guid MemberId { get; set; }
        public string FullName { get; set; }
        public string IdentityCode { get; set; }
        public string QrCodeUrl { get; set; }
    }

    // 3. Request: Chủ Family quét mã để thêm người này vào nhà
    public class AddMemberByIdentityRequest
    {
        public Guid TargetFamilyId { get; set; } // Add vào nhà nào
        public string IdentityCode { get; set; } // Mã lấy từ QR
    }
    public class MemberResponse
    {
        public Guid MemberId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? FamilyId { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Role { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public string IdentityCode { get; set; }
        public string? SyncToken { get; set; }
        public DateTime? SyncTokenExpireAt { get; set; }
    }

    public class UpdateMemberRequest
    {
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        /*public string? Role { get; set; }*/ // Chỉ Owner mới sửa được Role
        public IFormFile? AvatarFile { get; set; }
    }
    //request jion family by join code
    public class JoinFamilyRequest
    {
        public string JoinCode { get; set; } = string.Empty;

        // Chỉ cần truyền nếu là Người phụ thuộc (Dependent) chưa có tài khoản
        public Guid? ExistingMemberId { get; set; }
    }
}
