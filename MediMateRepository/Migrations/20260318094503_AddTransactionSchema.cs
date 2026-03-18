using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentId",
                table: "Transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "GatewayResponse",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Transactions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayoutId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionCode",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DoctorPayoutRates",
                columns: table => new
                {
                    RateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationType = table.Column<string>(type: "text", nullable: false),
                    AmountPerSession = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPayoutRates", x => x.RateId);
                });

            migrationBuilder.CreateTable(
                name: "DoctorPayouts",
                columns: table => new
                {
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPayouts", x => x.PayoutId);
                    table.ForeignKey(
                        name: "FK_DoctorPayouts_ConsultationSessions_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "ConsultationSessions",
                        principalColumn: "ConsultanSessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorPayouts_DoctorPayoutRates_RateId",
                        column: x => x.RateId,
                        principalTable: "DoctorPayoutRates",
                        principalColumn: "RateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PayoutId",
                table: "Transactions",
                column: "PayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionCode",
                table: "Transactions",
                column: "TransactionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPayouts_ConsultationId",
                table: "DoctorPayouts",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPayouts_RateId",
                table: "DoctorPayouts",
                column: "RateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_DoctorPayouts_PayoutId",
                table: "Transactions",
                column: "PayoutId",
                principalTable: "DoctorPayouts",
                principalColumn: "PayoutId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_DoctorPayouts_PayoutId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "DoctorPayouts");

            migrationBuilder.DropTable(
                name: "DoctorPayoutRates");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PayoutId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "GatewayResponse",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayoutId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "Transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
