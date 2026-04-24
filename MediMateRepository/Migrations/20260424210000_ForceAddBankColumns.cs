using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MediMateRepository.Migrations
{
    [DbContext(typeof(MediMateRepository.Data.MediMateDbContext))]
    [Migration("20260424210000_ForceAddBankColumns")]
    public partial class ForceAddBankColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sử dụng IF NOT EXISTS để đảm bảo không bị lỗi nếu cột đã có
            migrationBuilder.Sql(@"
                ALTER TABLE ""Clinics"" ADD COLUMN IF NOT EXISTS ""BankName"" text NOT NULL DEFAULT '';
                ALTER TABLE ""Clinics"" ADD COLUMN IF NOT EXISTS ""BankAccountNumber"" text NOT NULL DEFAULT '';
                ALTER TABLE ""Clinics"" ADD COLUMN IF NOT EXISTS ""BankAccountHolder"" text NOT NULL DEFAULT '';
                ALTER TABLE ""DoctorPayouts"" ADD COLUMN IF NOT EXISTS ""ReportFileUrl"" text;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tùy chọn drop column
        }
    }
}
