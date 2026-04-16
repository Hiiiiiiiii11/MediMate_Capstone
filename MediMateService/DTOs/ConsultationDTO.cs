namespace MediMateService.DTOs
{
    public class ConsultationSessionDto
    {
        public Guid ConsultanSessionId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? MemberAvatar { get; set; } // Bổ sung thêm avatar bệnh nhân

        // --- THÔNG TIN BỔ SUNG TỪ APPOINTMENT ---
        public DateTime AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public string? AppointmentStatus { get; set; }

        // --- THÔNG TIN BÁC SĨ (Cho trường hợp User xem) ---
        public string? DoctorName { get; set; }
        public string? DoctorAvatar { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;

        public bool UserJoined { get; set; }
        public bool DoctorJoined { get; set; }

        public string? Note { get; set; }
        public string? DoctorNote { get; set; }
    }

    public class EndConsultationDto
    {
        public DateTime? EndedAt { get; set; }
    }

    public class AttachPrescriptionDto
    {
        public Guid PrescriptionId { get; set; }
    }

    /// <summary>User hoặc Doctor join session (role = "user" | "doctor")</summary>
    public class JoinSessionDto
    {
        public string Role { get; set; } = "user";
    }

    /// <summary>Ghi nhận bác sĩ trễ X phút</summary>
    public class DoctorLateDto
    {
        public int LateMinutes { get; set; }
    }

    /// <summary>User huỷ vì không gặp bác sĩ</summary>
    public class CancelNoShowDto
    {
        // Không cần body, nhưng để dto cho nhất quán
    }

    /// <summary>Response khi Guardian (người giám hộ) tham gia cuộc gọi 3 bên</summary>
    public class GuardianJoinResponse
    {
        public string Token { get; set; } = string.Empty;
        public uint Uid { get; set; }
        public string ChannelName { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
    }
}
