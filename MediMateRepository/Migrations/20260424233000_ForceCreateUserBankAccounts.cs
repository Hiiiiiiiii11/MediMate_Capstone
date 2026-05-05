using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MediMateRepository.Migrations
{
    [DbContext(typeof(MediMateRepository.Data.MediMateDbContext))]
    [Migration("20260424233000_ForceCreateUserBankAccounts")]
    public partial class ForceCreateUserBankAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
            migrationBuilder.DropTable(
                name: "UserBankAccounts");
        }
    }
}
