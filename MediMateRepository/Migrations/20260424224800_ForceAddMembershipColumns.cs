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
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
