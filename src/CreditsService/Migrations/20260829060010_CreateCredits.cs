using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditsService.Migrations
{
    /// <inheritdoc />
    public partial class CreateCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TariffId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastInterestAccrualAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_credits_credit_tariffs_TariffId",
                        column: x => x.TariffId,
                        principalTable: "credit_tariffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_credit_operations_credits_CreditId",
                        column: x => x.CreditId,
                        principalTable: "credits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credit_operations_CreditId",
                table: "credit_operations",
                column: "CreditId");

            migrationBuilder.CreateIndex(
                name: "IX_credits_TariffId",
                table: "credits",
                column: "TariffId");

            migrationBuilder.CreateIndex(
                name: "IX_credits_UserId",
                table: "credits",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_operations");

            migrationBuilder.DropTable(
                name: "credits");
        }
    }
}
