using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.DTOs
{

    public class CreateHealthProfileRequest
    {
        // Có thể thêm [Required] nếu nghiệp vụ bắt buộc
        public string? BloodType { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public string? InsuranceNumber { get; set; }
    }
    // Request cập nhật thông tin sức khỏe cơ bản
    public class UpdateHealthProfileRequest
        {
            public string? BloodType { get; set; }
            public double Height { get; set; }
            public double Weight { get; set; }
            public string? InsuranceNumber { get; set; }
        }

        // Request thêm bệnh lý
        public class AddConditionRequest
        {
            public string ConditionName { get; set; }
            public string Description { get; set; }
            public DateTime DiagnosedDate { get; set; }
            public string Status { get; set; } = "Active";
        }

        // Response trả về full thông tin
        public class HealthProfileResponse
        {
            public Guid HealthProfileId { get; set; }
            public Guid MemberId { get; set; }
            public string BloodType { get; set; }
            public double Height { get; set; }
            public double Weight { get; set; }
            public double BMI => (Height > 0) ? Math.Round(Weight / ((Height / 100) * (Height / 100)), 2) : 0; // Tự tính BMI
            public string InsuranceNumber { get; set; }
            public List<HealthConditionDto> Conditions { get; set; }
    }

    public class HealthConditionDto
    {
        public Guid ConditionId { get; set; }
        public string ConditionName { get; set; }
        public string Description { get; set; }
        public DateTime DiagnosedDate { get; set; }
        public string Status { get; set; }
    }
    public class UpdateConditionRequest
    {
        public string? ConditionName { get; set; }
        public string? Description { get; set; }
        public DateTime? DiagnosedDate { get; set; }
        public string? Status { get; set; } // Active, Cured...
    }

    // Response hiển thị danh sách sức khỏe của cả gia đình (Dashboard)
    public class FamilyHealthSummaryResponse
    {
        public Guid MemberId { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public bool HasProfile { get; set; }
        public string BloodType { get; set; }
        public double BMI { get; set; }
        public int ActiveConditionsCount { get; set; }

        // SỬA Ở ĐÂY: Đổi từ List<string> sang List<HealthConditionDto>
        public List<HealthConditionDto> Conditions { get; set; } = new List<HealthConditionDto>();
    }
}
