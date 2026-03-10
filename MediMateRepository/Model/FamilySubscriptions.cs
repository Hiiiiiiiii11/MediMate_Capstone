using System.ComponentModel.DataAnnotations;

namespace MediMateRepository.Model
{
    public class FamilySubscriptions
    {
        [Key]
        public Guid SubscriptionId { get; set; } = Guid.NewGuid();
        public Guid FamilyId { get; set; }
        public Guid PackageId { get; set; }
        public Guid UserId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = "Active";
        public bool AutoRenew { get; set; } = false;

        public virtual Families Family { get; set; } = null!;
        public virtual MembershipPackages Package { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
