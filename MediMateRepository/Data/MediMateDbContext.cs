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
        // Doctor Booking
        public DbSet<Doctors> Doctors { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }
        public DbSet<Appointments> Appointments { get; set; }
        public DbSet<DoctorBankAccount> DoctorBankAccounts { get; set; }
        public DbSet<DoctorDocument> DoctorDocuments { get; set; }

        public DbSet<PrescriptionsByDoctor> PrescriptionsByDoctor { get; set; }
        public DbSet<Ratings> Ratings { get; set; }
        // Payment
        public DbSet<MembershipPackages> MembershipPackages { get; set; }
        public DbSet<FamilySubscriptions> FamilySubscriptions { get; set; }
        public DbSet<Payments> Payments { get; set; }
        public DbSet<Transactions> Transactions { get; set; }
        public DbSet<DoctorPayout> DoctorPayouts { get; set; }
        public DbSet<DoctorPayoutRate> DoctorPayoutRates { get; set; }
        public DbSet<ConsultationSessions> ConsultationSessions { get; set; }
        public DbSet<ChatDoctorMessages> ChatDoctorMessages { get; set; }

        public DbSet<RagBaseCollection> RagBaseCollections { get; set; } // THÊM MỚI
        public DbSet<RagBaseConfig> RagBaseConfigs { get; set; }         // THÊM MỚI
        public DbSet<RagBaseDocument> RagBaseDocuments { get; set; }     // THÊM MỚI
        public DbSet<RagBaseEmbedding> RagBaseEmbeddings { get; set; }
        public DbSet<Notifications> Notifications { get; set; }



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
            // Doctor
            modelBuilder.Entity<Doctors>().HasKey(d => d.DoctorId);
            modelBuilder.Entity<DoctorAvailability>().HasKey(da => da.DoctorAvailabilityId);
            modelBuilder.Entity<DoctorAvailabilityExceptions>().HasKey(da => da.ExceptionId);
            modelBuilder.Entity<Appointments>().HasKey(a => a.AppointmentId);
            modelBuilder.Entity<PrescriptionsByDoctor>().HasKey(pd => pd.DigitalPrescriptionId);
            modelBuilder.Entity<Ratings>().HasKey(r => r.RatingId);
            modelBuilder.Entity<DoctorBankAccount>().HasKey(dba => dba.BankAccountId);
            modelBuilder.Entity<DoctorDocument>().HasKey(dd => dd.DocumentId);
            // Payment
            modelBuilder.Entity<MembershipPackages>().HasKey(mp => mp.PackageId);
            modelBuilder.Entity<FamilySubscriptions>().HasKey(fs => fs.SubscriptionId);
            modelBuilder.Entity<Payments>().HasKey(p => p.PaymentId);
            modelBuilder.Entity<Transactions>().HasKey(t => t.TransactionId);
            modelBuilder.Entity<Transactions>().HasIndex(t => t.TransactionCode).IsUnique();
            modelBuilder.Entity<DoctorPayout>().HasKey(dp => dp.PayoutId);
            modelBuilder.Entity<DoctorPayoutRate>().HasKey(dpr => dpr.RateId);
            modelBuilder.Entity<ConsultationSessions>().HasKey(cs => cs.ConsultanSessionId);
            modelBuilder.Entity<ChatDoctorMessages>().HasKey(cm => cm.ChatDoctorMessageId);

            modelBuilder.Entity<RagBaseCollection>().HasKey(rc => rc.CollectionId);
            modelBuilder.Entity<RagBaseConfig>().HasKey(rc => rc.ConfigId);
            modelBuilder.Entity<RagBaseDocument>().HasKey(rd => rd.RagDocumentId);
            modelBuilder.Entity<RagBaseEmbedding>().HasKey(re => re.EmbeddingId);
            modelBuilder.Entity<Notifications>().HasKey(re => re.NotificationId);



            // --- USER CONFIGURATION ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique(); // SĐT là duy nh?t
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            // --- MEMBER CONFIGURATION ---
            // Quan h? 1-1: User <-> Member
            //modelBuilder.Entity<Members>()
            //    .HasOne<User>() // Member có th? liên k?t v?i User (không c?n property navigation ngư?c l?i ? User n?u không mu?n)
            //    .WithOne(u => u.MemberProfile) // User có 1 MemberProfile
            //    .HasForeignKey<Members>(m => m.UserId) // Khóa ngo?i là UserId trong b?ng Members
            //    .IsRequired(false) // UserId có th? null (cho ngư?i già/tr? em)
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
                .OnDelete(DeleteBehavior.Restrict); // Xóa User không xóa Family đ? gi? l?ch s?

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
                .WithMany() // N?u Member không c?n list Prescriptions th? đ? tr?ng, ho?c thêm prop vào Member
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Member -> Xóa đơn thu?c

            // 2. Prescriptions 1-N Images
            modelBuilder.Entity<PrescriptionImages>()
                .HasOne(img => img.Prescription)
                .WithMany(p => p.PrescriptionImages)
                .HasForeignKey(img => img.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa đơn -> Xóa ?nh

            // 3. Prescriptions 1-N Medicines
            modelBuilder.Entity<PrescriptionMedicines>()
                .HasOne(pm => pm.Prescription)
                .WithMany(p => p.PrescriptionMedicines) // Mapping v?i property Medications đ? s?a ? B1
                .HasForeignKey(pm => pm.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa đơn -> Xóa danh sách thu?c



            // 1. MedicationSchedules - Prescriptions (1-N)
            modelBuilder.Entity<MedicationSchedules>()
                .HasOne(ms => ms.Prescription)
                .WithMany(p => p.MedicationSchedules)
                .HasForeignKey(ms => ms.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa đơn thuốc -> Xóa lịch uống

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
                .WithMany(ms => ms.MedicationReminders) // Mapping ngư?c l?i
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
                .WithOne() // N?u b?ng Members b?n không khai báo `public virtual NotificationSetting Setting` th? đ? tr?ng WithOne()
                .HasForeignKey<NotificationSetting>(ns => ns.MemberId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Member -> T? đ?ng xóa Cài đ?t thông báo

            // Quan h? 1-N: Families -> ActivityLogs
            modelBuilder.Entity<ActivityLogs>()
                .HasOne(al => al.Family)
                .WithMany()
                .HasForeignKey(al => al.FamilyId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Family -> Xóa s?ch l?ch s? ho?t đ?ng c?a gia đ?nh đó

            // Quan h? 1-N: Members -> ActivityLogs
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

            // ==========================================
            // DOCTOR BOOKING RELATIONSHIPS
            // ==========================================

            // Doctors 1-N DoctorAvailability
            modelBuilder.Entity<DoctorAvailability>()
                 .HasOne(da => da.Doctor)
                 .WithMany(d => d.Availabilities)
                 .HasForeignKey(da => da.DoctorId)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorAvailabilityExceptions>()
                .HasOne(dae => dae.Doctor)
                .WithMany()
                .HasForeignKey(dae => dae.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
            // Doctors 1-N Appointments
            modelBuilder.Entity<Appointments>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Members 1-N Appointments
            modelBuilder.Entity<Appointments>()
                 .HasOne(a => a.Member)
                 .WithMany()
                 .HasForeignKey(a => a.MemberId)
                 .OnDelete(DeleteBehavior.Cascade);

            // Appointments 1-1 ConsultationSessions
            modelBuilder.Entity<ConsultationSessions>()
                .HasOne(cs => cs.Appointment) // Chỉ định rõ Navigation Property
                .WithOne()                    // Sửa WithMany() thành WithOne() vì đây là quan hệ 1-1
                .HasForeignKey<ConsultationSessions>(cs => cs.AppointmentId) // Phải xác định rõ Type chứa Khóa ngoại
                .OnDelete(DeleteBehavior.Restrict);

            // ConsultationSessions 1-N PrescriptionsByDoctor
            modelBuilder.Entity<PrescriptionsByDoctor>()
                .HasOne(pd => pd.Session)
                .WithMany()
                .HasForeignKey(pd => pd.ConsultanSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ConsultationSessions 1-N Ratings
            modelBuilder.Entity<Ratings>()
                .HasOne(r => r.ConsultationSession)
                .WithMany()
                .HasForeignKey(r => r.ConsultanSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Ratings>()
                .HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Ratings>()
                .HasOne(r => r.Member)
                .WithMany()
                .HasForeignKey(r => r.MemberId)
                .OnDelete(DeleteBehavior.NoAction);

            // Doctors 1-1 Users
            modelBuilder.Entity<Doctors>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Doctors 1-N DoctorBankAccount (THÊM MỚI)
            modelBuilder.Entity<DoctorBankAccount>()
                .HasOne(dba => dba.Doctor)
                .WithMany()
                .HasForeignKey(dba => dba.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Doctors 1-N DoctorDocument (THÊM MỚI)
            modelBuilder.Entity<DoctorDocument>()
                .HasOne(dd => dd.Doctor)
                .WithMany()
                .HasForeignKey(dd => dd.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointments>()
                .HasOne(a => a.Availability)
                .WithMany()
                .HasForeignKey(a => a.AvailabilityId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xóa khung giờ nếu đã có người đặt lịch

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

            // Payments 1-N Transactions (nullable FK)
            modelBuilder.Entity<Transactions>()
                .HasOne(t => t.Payment)
                .WithMany()
                .HasForeignKey(t => t.PaymentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorPayoutRate 1-N DoctorPayout
            modelBuilder.Entity<DoctorPayout>()
                .HasOne(dp => dp.Rate)
                .WithMany()
                .HasForeignKey(dp => dp.RateId)
                .OnDelete(DeleteBehavior.Restrict);

            // ConsultationSessions 1-N DoctorPayout
            modelBuilder.Entity<DoctorPayout>()
                .HasOne(dp => dp.ConsultationSession)
                .WithMany()
                .HasForeignKey(dp => dp.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorPayout 1-N Transactions (nullable FK)
            modelBuilder.Entity<Transactions>()
                .HasOne(t => t.Payout)
                .WithMany()
                .HasForeignKey(t => t.PayoutId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<ChatDoctorMessages>()
            //    .HasOne(m => m.Sender)
            //    .WithMany()
            //    .HasForeignKey(m => m.SenderId)
            //    .IsRequired(false)
            //    .OnDelete(DeleteBehavior.NoAction);
            //modelBuilder.Entity<ChatDoctorMessages>()
            //    .HasOne(m => m.Sender)
            //    .WithMany()
            //    .HasForeignKey(m => m.SenderId)
            //    .IsRequired(false)
            //    .OnDelete(DeleteBehavior.NoAction);

            //modelBuilder.Entity<ChatDoctorMessages>()
            //    .HasOne(m => m.DoctorSender)
            //    .WithMany()
            //    .HasForeignKey(m => m.SenderId)
            //    .IsRequired(false)
            //    .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatDoctorMessages>()
                .HasOne(m => m.ConsultantSession)
                .WithMany(cs => cs.Messages) // Map vào ICollection trong Session
                .HasForeignKey(m => m.ConsultanSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1. Collection 1-N Documents
            modelBuilder.Entity<RagBaseDocument>()
                .HasOne(rd => rd.RagBaseCollection)
                .WithMany() // Nếu Collection có list Documents thì map vào đây, vd: .WithMany(c => c.Documents)
                .HasForeignKey(rd => rd.CollectionId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Collection -> Xóa sạch các tài liệu bên trong

            // 2. Document 1-N Embeddings
            modelBuilder.Entity<RagBaseEmbedding>()
                .HasOne(re => re.RagBaseDocument)
                .WithMany() // Tương tự, nếu Document có list Embeddings thì map vào
                .HasForeignKey(re => re.RagDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }

}
//dotnet ef migrations add InitialDB --project MediMateRepository --startup-project MediMate
//dotnet ef database update --project MediMateRepository --startup-project MediMate

