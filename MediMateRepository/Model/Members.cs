using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class Members
    {
        public Guid MemberId { get; set; } = Guid.NewGuid();
        public Guid? FamilyId { get; set; }
        public Guid? UserId { get; set; }
        public string? FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Role { get; set; }
        public string? IdentityCode { get; set; }
        public string? AvatarUrl { get; set; }
        public string? SyncToken { get; set; }
        public DateTime? SyncTokenExpireAt { get; set; }
        public string? FcmToken { get; set; }
        public bool IsActive { get; set; }
        public virtual HealthProfiles? HealthProfile { get; set; }
    }
}
