using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetWise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCategoryExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_category_exclusions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_category_exclusions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_category_exclusions_user_category",
                table: "user_category_exclusions",
                columns: new[] { "user_id", "category_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_category_exclusions");
        }
    }
}
