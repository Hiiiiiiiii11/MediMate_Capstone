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
        public DbSet<ChatbotSession> ChatbotSessions { get; set; }
        public DbSet<ChatbotMessages> ChatbotMessages { get; set; }


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
            modelBuilder.Entity<ChatbotSession>().HasKey(cs => cs.BotSessionId);
            modelBuilder.Entity<ChatbotMessages>().HasKey(cm => cm.BotMessageId);



            // --- USER CONFIGURATION ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique(); // SĐT là duy nhất
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            // --- MEMBER CONFIGURATION ---
            // Quan hệ 1-1: User <-> Member
            //modelBuilder.Entity<Members>()
            //    .HasOne<User>() // Member có thể liên kết với User (không cần property navigation ngược lại ở User nếu không muốn)
            //    .WithOne(u => u.MemberProfile) // User có 1 MemberProfile
            //    .HasForeignKey<Members>(m => m.UserId) // Khóa ngoại là UserId trong bảng Members
            //    .IsRequired(false) // UserId có thể null (cho người già/trẻ em)
            //    .OnDelete(DeleteBehavior.SetNull); // Xóa User thì set UserId về null

            // Quan hệ 1-N: Family -> Members
            modelBuilder.Entity<Members>()
                .HasOne<Families>() // Member thuộc về 1 Family (dùng shadow navigation hoặc thêm prop Family vào Member nếu cần)
                .WithMany(f => f.FamilyMembers) // Family có nhiều Member
                .HasForeignKey(m => m.FamilyId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Family -> Xóa hết Member

            // --- FAMILY CONFIGURATION ---
            // Quan hệ 1-N: User -> Created Families
            modelBuilder.Entity<Families>()
                .HasOne(f => f.Creator)
                .WithMany(u => u.CreatedFamilies)
                .HasForeignKey(f => f.CreateBy)
                .OnDelete(DeleteBehavior.Restrict); // Xóa User không xóa Family để giữ lịch sử

            modelBuilder.Entity<Members>()
        .HasOne(m => m.HealthProfile)
        .WithOne(hp => hp.Member)
        .HasForeignKey<HealthProfiles>(hp => hp.MemberId)
        .OnDelete(DeleteBehavior.Cascade); // Xóa Member thì xóa luôn HealthProfile

            // Cấu hình 1-N: HealthProfile - HealthConditions
            modelBuilder.Entity<HealthProfiles>()
                .HasMany(hp => hp.Conditions)
                .WithOne(c => c.HealthProfile)
                .HasForeignKey(c => c.HealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);


            // 1. Members 1-N Prescriptions
            modelBuilder.Entity<Prescriptions>()
                .HasOne(p => p.Member)
                .WithMany() // Nếu Member không cần list Prescriptions thì để trống, hoặc thêm prop vào Member
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Member -> Xóa đơn thuốc

            // 2. Prescriptions 1-N Images
            modelBuilder.Entity<PrescriptionImages>()
                .HasOne(img => img.Prescription)
                .WithMany(p => p.PrescriptionImages)
                .HasForeignKey(img => img.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa đơn -> Xóa ảnh

            // 3. Prescriptions 1-N Medicines
            modelBuilder.Entity<PrescriptionMedicines>()
                .HasOne(pm => pm.Prescription)
                .WithMany(p => p.PrescriptionMedicines) // Mapping với property Medications đã sửa ở B1
                .HasForeignKey(pm => pm.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa đơn -> Xóa danh sách thuốc



            // 1. MedicationSchedules - PrescriptionMedicines (1-1 hoặc 1-N tùy logic)
            // Ở đây bạn đang để 1 Schedule ứng với 1 PrescriptionMedicineId
            modelBuilder.Entity<MedicationSchedules>()
                .HasOne(ms => ms.PrescriptionMedicines)
                .WithMany() // Một loại thuốc trong đơn có thể có nhiều lịch uống (hoặc 1, tùy bạn)
                .HasForeignKey(ms => ms.PrescriptionMedicineId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa thuốc trong đơn -> Xóa lịch uống

            // 2. MedicationSchedules - Members (1-N)
            modelBuilder.Entity<MedicationSchedules>()
                .HasOne(ms => ms.Member)
                .WithMany() // Member có nhiều lịch uống
                .HasForeignKey(ms => ms.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- MEDICATION REMINDERS CONFIGURATION ---

            // 1. MedicationSchedules - MedicationReminders (1-N)
            modelBuilder.Entity<MedicationReminders>()
                .HasOne(mr => mr.Schedule)
                .WithMany(ms => ms.MedicationReminders) // Mapping ngược lại
                .HasForeignKey(mr => mr.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa lịch -> Xóa các nhắc nhở con

            // --- MEDICATION LOGS CONFIGURATION ---

            // 1. MedicationLogs - Members (1-N)
            modelBuilder.Entity<MedicationLogs>()
                .HasOne(ml => ml.Member)
                .WithMany()
                .HasForeignKey(ml => ml.MemberId)
                .OnDelete(DeleteBehavior.NoAction); // Tránh vòng lặp cascade (Member -> Log)

            // 2. MedicationLogs - MedicationSchedules (1-N)
            modelBuilder.Entity<MedicationLogs>()
                .HasOne(ml => ml.Schedule)
                .WithMany()
                .HasForeignKey(ml => ml.ScheduleId)
                .OnDelete(DeleteBehavior.NoAction); // Tránh vòng lặp cascade

            // 3. MedicationLogs - MedicationReminders (1-N)
            modelBuilder.Entity<MedicationLogs>()
                .HasOne(ml => ml.Reminder)
                .WithMany()
                .HasForeignKey(ml => ml.ReminderId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa nhắc nhở -> Xóa log

            // Quan hệ 1-1: Members <-> NotificationSetting
            modelBuilder.Entity<NotificationSetting>()
                .HasOne(ns => ns.Member)
                .WithOne() // Nếu bảng Members bạn không khai báo `public virtual NotificationSetting Setting` thì để trống WithOne()
                .HasForeignKey<NotificationSetting>(ns => ns.MemberId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Member -> Tự động xóa Cài đặt thông báo

            // Quan hệ 1-N: Families -> ActivityLogs
            modelBuilder.Entity<ActivityLogs>()
                .HasOne(al => al.Family)
                .WithMany()
                .HasForeignKey(al => al.FamilyId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Family -> Xóa sạch lịch sử hoạt động của gia đình đó

            // Quan hệ 1-N: Members -> ActivityLogs
            modelBuilder.Entity<ActivityLogs>()
                .HasOne(al => al.Member)
                .WithMany()
                .HasForeignKey(al => al.MemberId)
                .OnDelete(DeleteBehavior.NoAction); // QUAN TRỌNG: Dùng NoAction để tránh lỗi vòng lặp Cascade (Đụng độ với lệnh Xóa Family ở trên).
                                                    // Nếu Member bị xóa, Log vẫn còn giữ lại để chủ hộ biết "Ai đó đã từng làm gì", nhưng ta phải tự xử lý hiển thị MemberId bị null/mất tích trên UI.
            modelBuilder.Entity<ChatbotSession>()
                .HasOne(cs => cs.Member)
                .WithMany() // Nếu bảng Members không có list Sessions thì để trống
                .HasForeignKey(cs => cs.MemberId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Member -> Xóa tất cả các phiên chat của họ

            // 2. Quan hệ 1-N: ChatbotSession -> ChatbotMessages
            modelBuilder.Entity<ChatbotMessages>()
                .HasOne(cm => cm.Session)
                .WithMany(cs => cs.Messages) // Map ngược lại list Messages trong ChatbotSession
                .HasForeignKey(cm => cm.BotSessionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Session -> Xóa sạch tin nhắn trong session đó
        }
    }
}
//dotnet ef migrations add InitialDB --project MediMateRepository --startup-project MediMate
//dotnet ef database update --project MediMateRepository --startup-project MediMate