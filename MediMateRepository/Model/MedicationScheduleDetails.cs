using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class MedicationScheduleDetails
    {
        public Guid ScheduleDetailId { get; set; }
        public Guid ScheduleId { get; set; } // Nằm trong khung giờ nào (8h, 14h...)
        public Guid PrescriptionMedicineId { get; set; } // Link tới loại thuốc trong đơn

        public string Dosage { get; set; } = string.Empty; // VD: "1 viên"

        public DateTime StartDate { get; set; } // Ngày bắt đầu uống
        public DateTime EndDate { get; set; }   // Ngày uống viên cuối cùng (Tính từ Qty)

        public virtual MedicationSchedules Schedule { get; set; }
        public virtual PrescriptionMedicines PrescriptionMedicine { get; set; }
    }
}
