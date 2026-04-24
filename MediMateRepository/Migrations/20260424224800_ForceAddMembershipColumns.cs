using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MediMateRepository.Migrations
{
    [DbContext(typeof(MediMateRepository.Data.MediMateDbContext))]
    [Migration("20260424224800_ForceAddMembershipColumns")]
    public partial class ForceAddMembershipColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""MembershipPackages"" ADD COLUMN IF NOT EXISTS ""AllowVideoRecordingAccess"" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE ""MembershipPackages"" ADD COLUMN IF NOT EXISTS ""HealthAlertEnabled"" boolean NOT NULL DEFAULT FALSE;


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
        }
    }
}
