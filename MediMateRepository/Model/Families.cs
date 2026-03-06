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
        public FamilyType Type { get; set; } = FamilyType.Shared;
        public string? JoinCode { get; set; }
        public bool IsOpenJoin { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
        public virtual ICollection<Members> FamilyMembers { get; set; } = new List<Members>();
        public enum FamilyType
        {
            Personal = 0, // Chế độ cá nhân (Chỉ 1 thành viên)
            Shared = 1    // Chế độ gia đình (Nhiều thành viên)
        }
    }
}
