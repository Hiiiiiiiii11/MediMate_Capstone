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
    }

    // 2. Response: Trả về QR Code chứa IdentityCode
    public class InitDependentResponse
    {
        public Guid MemberId { get; set; }
        public string FullName { get; set; }
        public string IdentityCode { get; set; } // Mã text (VD: MEM-1234)
        public string QrCodeBase64 { get; set; } // Ảnh QR
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
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Role { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateMemberRequest
    {
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        /*public string? Role { get; set; }*/ // Chỉ Owner mới sửa được Role
    }
}
