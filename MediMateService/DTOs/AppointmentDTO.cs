namespace MediMateService.DTOs
{
    public class CreateAppointmentDto
    {
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public Guid AvailabilityId { get; set; }
        public DateTime AppointmentDate { get; set; }
    }

    public class CancelAppointmentDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AppointmentDto
    {
        public Guid AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid MemberId { get; set; }
        public Guid AvailabilityId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }


    public class AvailableSlotDto
    {
        public int Time { get; set; } // Giờ khám (ví dụ: 8, 9, 14...)
        public string DisplayTime { get; set; } = string.Empty; // Chuỗi hiển thị (ví dụ: "08:00 - 09:00")
        public bool IsBooked { get; set; } // true: Đã có người đặt, false: Còn trống
    }
}
