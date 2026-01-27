using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class Families
    {
        public Guid FamilyId { get; set; } = Guid.NewGuid();
        public string? FamilyName { get; set; }
        public Guid CreateBy { get; set; }
        public string? JoinCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
        public virtual ICollection<Members> FamilyMembers { get; set; } = new List<Members>();
    }
}
