using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetWise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLockoutColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "access_failed_count",
                table: "asp_net_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "lockout_enabled",
                table: "asp_net_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end",
                table: "asp_net_users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_failed_count",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "lockout_enabled",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "lockout_end",
                table: "asp_net_users");
        }
    }
}
