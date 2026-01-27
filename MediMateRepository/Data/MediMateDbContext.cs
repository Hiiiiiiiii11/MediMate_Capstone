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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Families>().HasKey(f => f.FamilyId); 
            modelBuilder.Entity<Members>().HasKey(m => m.MemberId);

            // --- USER CONFIGURATION ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique(); // SĐT là duy nhất

            // --- MEMBER CONFIGURATION ---
            // Quan hệ 1-1: User <-> Member
            modelBuilder.Entity<Members>()
                .HasOne<User>() // Member có thể liên kết với User (không cần property navigation ngược lại ở User nếu không muốn)
                .WithOne(u => u.MemberProfile) // User có 1 MemberProfile
                .HasForeignKey<Members>(m => m.UserId) // Khóa ngoại là UserId trong bảng Members
                .IsRequired(false) // UserId có thể null (cho người già/trẻ em)
                .OnDelete(DeleteBehavior.SetNull); // Xóa User thì set UserId về null

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
        }
    }
}
//dotnet ef migrations add InitialDB --project MediMateRepository --startup-project MediMate
//dotnet ef database update --project MediMateRepository --startup-project MediMate