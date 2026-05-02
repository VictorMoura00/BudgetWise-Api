using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetWise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_type",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill system categories — personal categories keep Both (0).
            // Expense = 1, Income = 2 (Both = 0 is already set by the column default).
            migrationBuilder.Sql("""
                UPDATE categories SET category_type = 1
                WHERE is_system = true
                  AND name IN ('Alimentação','Transporte','Moradia','Saúde','Lazer','Educação');

                UPDATE categories SET category_type = 2
                WHERE is_system = true
                  AND name IN ('Salário','Freelance');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category_type",
                table: "categories");
        }
    }
}
