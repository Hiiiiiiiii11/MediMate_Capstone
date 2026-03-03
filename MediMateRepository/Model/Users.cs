using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Share.Constants;

namespace MediMateRepository.Model
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Role { get; set; } = Roles.User;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? FcmToken { get; set; } = string.Empty;
        public int? VerifyCode { get; set; }
        public DateTime? ExpiriedAt { get; set; }
        public virtual ICollection<Families> CreatedFamilies { get; set; } = new List<Families>();
        public virtual ICollection<Members> MemberProfiles { get; set; } = new List<Members>();
    }
}
