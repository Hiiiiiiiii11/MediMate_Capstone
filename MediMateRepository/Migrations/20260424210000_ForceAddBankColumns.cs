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
                ALTER TABLE ""Clinics"" ADD COLUMN IF NOT EXISTS ""Email"" text;
                ALTER TABLE ""DoctorPayouts"" ADD COLUMN IF NOT EXISTS ""ReportFileUrl"" text;

                CREATE TABLE IF NOT EXISTS ""UserBankAccounts"" (
                    ""BankAccountId"" uuid NOT NULL DEFAULT gen_random_uuid(),
                    ""UserId"" uuid NOT NULL,
                    ""BankName"" text NOT NULL DEFAULT '',
                    ""AccountNumber"" text NOT NULL DEFAULT '',
                    ""AccountHolder"" text NOT NULL DEFAULT '',
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT now(),
                    ""UpdatedAt"" timestamp without time zone NOT NULL DEFAULT now(),
                    CONSTRAINT ""PK_UserBankAccounts"" PRIMARY KEY (""BankAccountId""),
                    CONSTRAINT ""FK_UserBankAccounts_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users""(""UserId"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_UserBankAccounts_UserId"" ON ""UserBankAccounts""(""UserId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tùy chọn drop column
        }
    }
}
