using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class Prescriptions
    {
        public Guid PrescriptionId { get; set; }
        public Guid MemberId { get; set; }
        public string PrescriptionCode { get; set; }
        public string DoctorName { get; set; }
        public string HospitalName { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public virtual Members Member { get; set; }
        public virtual ICollection<PrescriptionImages> PrescriptionImages { get; set; } = new List<PrescriptionImages>();
        public virtual ICollection<PrescriptionMedicines> PrescriptionMedicines { get; set; } = new List<PrescriptionMedicines>();
        public virtual ICollection<MedicationSchedules> MedicationSchedules { get; set; } = new List<MedicationSchedules>();

    }
}
