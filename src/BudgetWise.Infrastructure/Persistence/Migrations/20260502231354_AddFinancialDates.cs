using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetWise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "due_date",
                table: "transactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "paid_at",
                table: "transactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_transactions_paid_at",
                table: "transactions",
                sql: "is_confirmed = false OR paid_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_transactions_paid_at",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "transactions");
        }
    }
}
