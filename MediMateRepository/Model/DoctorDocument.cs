using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class DoctorDocument
    {
        public Guid DocumentId { get; set; }
        public Guid DoctorId { get; set; }
        public string FileUrl { get; set; }
        public string Type { get; set; }
        public string? Status { get; set; }
        public string? ReviewBy { get; set; }
        public string? ReviewAt { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual Doctors Doctor { get; set; }
    }
}
