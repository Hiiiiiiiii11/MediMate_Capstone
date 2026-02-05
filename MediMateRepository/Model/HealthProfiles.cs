using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class HealthProfiles
    {
        public Guid HealthProfileId { get; set; }
        public Guid MemberId { get; set; }
        public string BloodType { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Weight { get; set; }
        public string InsuranceNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public virtual Members Member { get; set; }
        public virtual ICollection<HealthConditions> Conditions { get; set; } = new List<HealthConditions>();

    }
}
