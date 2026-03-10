using MediMateRepository.Model;
using Microsoft.EntityFrameworkCore;

namespace MediMateRepository.Data
{
    public class MediMateDbContext : DbContext
    {
        public MediMateDbContext(DbContextOptions<MediMateDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Families> Families { get; set; }
        public DbSet<Members> Members { get; set; }
        public DbSet<HealthProfiles> HealthProfiles { get; set; }
        public DbSet<HealthConditions> HealthConditions { get; set; }
        public DbSet<Prescriptions> Prescriptions { get; set; }
        public DbSet<PrescriptionMedicines> PrescriptionMedicines { get; set; }
        public DbSet<PrescriptionImages> PrescriptionImages { get; set; }
        public DbSet<MedicationLogs> MedicationLogs { get; set; }
        public DbSet<MedicationSchedules> MedicationSchedules { get; set; }
        public DbSet<MedicationReminders> MedicationReminders { get; set; }
        public DbSet<NotificationSetting> NotificationSettings { get; set; }
        public DbSet<ActivityLogs> ActivityLogs { get; set; }
        // Doctor Booking
        public DbSet<Doctors> Doctors { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }
        public DbSet<Appointments> Appointments { get; set; }
        public DbSet<ConsultationSessions> ConsultationSessions { get; set; }
        public DbSet<PrescriptionsByDoctor> PrescriptionsByDoctor { get; set; }
        public DbSet<Ratings> Ratings { get; set; }
        // Payment
        public DbSet<MembershipPackages> MembershipPackages { get; set; }
        public DbSet<FamilySubscriptions> FamilySubscriptions { get; set; }
        public DbSet<Payments> Payments { get; set; }
        public DbSet<Transactions> Transactions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Families>().HasKey(f => f.FamilyId);
            modelBuilder.Entity<Members>().HasKey(m => m.MemberId);
            modelBuilder.Entity<HealthProfiles>().HasKey(hp => hp.HealthProfileId);
            modelBuilder.Entity<HealthConditions>().HasKey(hc => hc.ConditionId);
            modelBuilder.Entity<Prescriptions>().HasKey(p => p.PrescriptionId);
            modelBuilder.Entity<PrescriptionMedicines>().HasKey(pm => pm.PrescriptionMedicineId);
            modelBuilder.Entity<PrescriptionImages>().HasKey(pi => pi.ImageId);
            modelBuilder.Entity<MedicationLogs>().HasKey(ml => ml.LogId);
            modelBuilder.Entity<MedicationSchedules>().HasKey(ms => ms.ScheduleId);
            modelBuilder.Entity<MedicationReminders>().HasKey(mr => mr.ReminderId);
            modelBuilder.Entity<NotificationSetting>().HasKey(ns => ns.SettingId);
            modelBuilder.Entity<ActivityLogs>().HasKey(al => al.LogId);
            // Doctor
            modelBuilder.Entity<Doctors>().HasKey(d => d.DoctorId);
            modelBuilder.Entity<DoctorAvailability>().HasKey(da => da.DoctorAvailabilityId);
            modelBuilder.Entity<Appointments>().HasKey(a => a.AppointmentId);
            modelBuilder.Entity<ConsultationSessions>().HasKey(cs => cs.SessionId);
            modelBuilder.Entity<PrescriptionsByDoctor>().HasKey(pd => pd.DigitalPrescriptionId);
            modelBuilder.Entity<Ratings>().HasKey(r => r.RatingId);
            // Payment
            modelBuilder.Entity<MembershipPackages>().HasKey(mp => mp.PackageId);
            modelBuilder.Entity<FamilySubscriptions>().HasKey(fs => fs.SubscriptionId);
            modelBuilder.Entity<Payments>().HasKey(p => p.PaymentId);
            modelBuilder.Entity<Transactions>().HasKey(t => t.TransactionId);



            // --- USER CONFIGURATION ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique(); // SÐT là duy nh?t
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            // --- MEMBER CONFIGURATION ---
            // Quan h? 1-1: User <-> Member
            //modelBuilder.Entity<Members>()
            //    .HasOne<User>() // Member có th? liên k?t v?i User (không c?n property navigation ngý?c l?i ? User n?u không mu?n)
            //    .WithOne(u => u.MemberProfile) // User có 1 MemberProfile
            //    .HasForeignKey<Members>(m => m.UserId) // Khóa ngo?i là UserId trong b?ng Members
            //    .IsRequired(false) // UserId có th? null (cho ngý?i già/tr? em)
            //    .OnDelete(DeleteBehavior.SetNull); // Xóa User th? set UserId v? null

            // Quan h? 1-N: Family -> Members
            modelBuilder.Entity<Members>()
                .HasOne<Families>() // Member thu?c v? 1 Family (dùng shadow navigation ho?c thêm prop Family vào Member n?u c?n)
                .WithMany(f => f.FamilyMembers) // Family có nhi?u Member
                .HasForeignKey(m => m.FamilyId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Family -> Xóa h?t Member

            // --- FAMILY CONFIGURATION ---
            // Quan h? 1-N: User -> Created Families
            modelBuilder.Entity<Families>()
                .HasOne(f => f.Creator)
                .WithMany(u => u.CreatedFamilies)
                .HasForeignKey(f => f.CreateBy)
                .OnDelete(DeleteBehavior.Restrict); // Xóa User không xóa Family ð? gi? l?ch s?

            modelBuilder.Entity<Members>()
        .HasOne(m => m.HealthProfile)
        .WithOne(hp => hp.Member)
        .HasForeignKey<HealthProfiles>(hp => hp.MemberId)
        .OnDelete(DeleteBehavior.Cascade); // Xóa Member th? xóa luôn HealthProfile

            // C?u h?nh 1-N: HealthProfile - HealthConditions
            modelBuilder.Entity<HealthProfiles>()
                .HasMany(hp => hp.Conditions)
                .WithOne(c => c.HealthProfile)
                .HasForeignKey(c => c.HealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);


            // 1. Members 1-N Prescriptions
            modelBuilder.Entity<Prescriptions>()
                .HasOne(p => p.Member)
                .WithMany() // N?u Member không c?n list Prescriptions th? ð? tr?ng, ho?c thêm prop vào Member
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Member -> Xóa ðõn thu?c

            // 2. Prescriptions 1-N Images
            modelBuilder.Entity<PrescriptionImages>()
                .HasOne(img => img.Prescription)
                .WithMany(p => p.PrescriptionImages)
                .HasForeignKey(img => img.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa ðõn -> Xóa ?nh

            // 3. Prescriptions 1-N Medicines
            modelBuilder.Entity<PrescriptionMedicines>()
                .HasOne(pm => pm.Prescription)
                .WithMany(p => p.PrescriptionMedicines) // Mapping v?i property Medications ð? s?a ? B1
                .HasForeignKey(pm => pm.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa ðõn -> Xóa danh sách thu?c



            // 1. MedicationSchedules - PrescriptionMedicines (1-1 ho?c 1-N tùy logic)
            // ? ðây b?n ðang ð? 1 Schedule ?ng v?i 1 PrescriptionMedicineId
            modelBuilder.Entity<MedicationSchedules>()
                .HasOne(ms => ms.PrescriptionMedicines)
                .WithMany() // M?t lo?i thu?c trong ðõn có th? có nhi?u l?ch u?ng (ho?c 1, tùy b?n)
                .HasForeignKey(ms => ms.PrescriptionMedicineId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa thu?c trong ðõn -> Xóa l?ch u?ng

            // 2. MedicationSchedules - Members (1-N)
            modelBuilder.Entity<MedicationSchedules>()
                .HasOne(ms => ms.Member)
                .WithMany() // Member có nhi?u l?ch u?ng
                .HasForeignKey(ms => ms.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- MEDICATION REMINDERS CONFIGURATION ---

            // 1. MedicationSchedules - MedicationReminders (1-N)
            modelBuilder.Entity<MedicationReminders>()
                .HasOne(mr => mr.Schedule)
                .WithMany(ms => ms.MedicationReminders) // Mapping ngý?c l?i
                .HasForeignKey(mr => mr.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa l?ch -> Xóa các nh?c nh? con

            // --- MEDICATION LOGS CONFIGURATION ---

            // 1. MedicationLogs - Members (1-N)
            modelBuilder.Entity<MedicationLogs>()
                .HasOne(ml => ml.Member)
                .WithMany()
                .HasForeignKey(ml => ml.MemberId)
                .OnDelete(DeleteBehavior.NoAction); // Tránh v?ng l?p cascade (Member -> Log)

            // 2. MedicationLogs - MedicationSchedules (1-N)
            modelBuilder.Entity<MedicationLogs>()
                .HasOne(ml => ml.Schedule)
                .WithMany()
                .HasForeignKey(ml => ml.ScheduleId)
                .OnDelete(DeleteBehavior.NoAction); // Tránh v?ng l?p cascade

            // 3. MedicationLogs - MedicationReminders (1-N)
            modelBuilder.Entity<MedicationLogs>()
                .HasOne(ml => ml.Reminder)
                .WithMany()
                .HasForeignKey(ml => ml.ReminderId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa nh?c nh? -> Xóa log

            // Quan h? 1-1: Members <-> NotificationSetting
            modelBuilder.Entity<NotificationSetting>()
                .HasOne(ns => ns.Member)
                .WithOne() // N?u b?ng Members b?n không khai báo `public virtual NotificationSetting Setting` th? ð? tr?ng WithOne()
                .HasForeignKey<NotificationSetting>(ns => ns.MemberId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Member -> T? ð?ng xóa Cài ð?t thông báo

            // Quan h? 1-N: Families -> ActivityLogs
            modelBuilder.Entity<ActivityLogs>()
                .HasOne(al => al.Family)
                .WithMany()
                .HasForeignKey(al => al.FamilyId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Family -> Xóa s?ch l?ch s? ho?t ð?ng c?a gia ð?nh ðó

            // Quan h? 1-N: Members -> ActivityLogs
            modelBuilder.Entity<ActivityLogs>()
                .HasOne(al => al.Member)
                .WithMany()
                .HasForeignKey(al => al.MemberId)
                .OnDelete(DeleteBehavior.NoAction);

            // ==========================================
            // DOCTOR BOOKING RELATIONSHIPS
            // ==========================================

            // Doctors 1-N DoctorAvailability
            modelBuilder.Entity<DoctorAvailability>()
                .HasOne<Doctors>()
                .WithMany()
                .HasForeignKey(da => da.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Doctors 1-N Appointments
            modelBuilder.Entity<Appointments>()
                .HasOne<Doctors>()
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Members 1-N Appointments
            modelBuilder.Entity<Appointments>()
                .HasOne<Members>()
                .WithMany()
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointments 1-1 ConsultationSessions
            modelBuilder.Entity<ConsultationSessions>()
                .HasOne<Appointments>()
                .WithMany()
                .HasForeignKey(cs => cs.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ConsultationSessions 1-N PrescriptionsByDoctor
            modelBuilder.Entity<PrescriptionsByDoctor>()
                .HasOne(pd => pd.Session)
                .WithMany()
                .HasForeignKey(pd => pd.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ConsultationSessions 1-N Ratings
            modelBuilder.Entity<Ratings>()
                .HasOne<ConsultationSessions>()
                .WithMany()
                .HasForeignKey(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Doctors 1-1 Users
            modelBuilder.Entity<Doctors>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // PAYMENT RELATIONSHIPS
            // ==========================================

            // Families 1-N FamilySubscriptions
            modelBuilder.Entity<FamilySubscriptions>()
                .HasOne(fs => fs.Family)
                .WithMany()
                .HasForeignKey(fs => fs.FamilyId)
                .OnDelete(DeleteBehavior.Cascade);

            // MembershipPackages 1-N FamilySubscriptions
            modelBuilder.Entity<FamilySubscriptions>()
                .HasOne(fs => fs.Package)
                .WithMany()
                .HasForeignKey(fs => fs.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            // FamilySubscriptions 1-N Payments
            modelBuilder.Entity<Payments>()
                .HasOne(p => p.Subscription)
                .WithMany()
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payments 1-N Transactions
            modelBuilder.Entity<Transactions>()
                .HasOne(t => t.Payment)
                .WithMany()
                .HasForeignKey(t => t.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
//dotnet ef migrations add InitialDB --project MediMateRepository --startup-project MediMate
//dotnet ef database update --project MediMateRepository --startup-project MediMate