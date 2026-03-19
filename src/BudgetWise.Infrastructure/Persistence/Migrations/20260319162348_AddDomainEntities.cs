using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetWise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.CheckConstraint("ck_categories_color", "color ~ '^#[0-9A-Fa-f]{6}$' OR color IS NULL");
                });

            migrationBuilder.CreateTable(
                name: "family_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    invite_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_family_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "family_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    family_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_family_members", x => x.id);
                    table.CheckConstraint("ck_family_members_role", "role IN ('Owner', 'Member')");
                    table.ForeignKey(
                        name: "fk_family_members_family_groups_family_group_id",
                        column: x => x.family_group_id,
                        principalTable: "family_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    recurrence_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "None"),
                    recurrence_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                    table.CheckConstraint("ck_transactions_amount", "amount > 0");
                    table.CheckConstraint("ck_transactions_payment_method", "payment_method IN ('Pix', 'CreditCard', 'DebitCard', 'Cash', 'Ted', 'Boleto', 'Other') OR payment_method IS NULL");
                    table.CheckConstraint("ck_transactions_recurrence_type", "recurrence_type IN ('None', 'Daily', 'Weekly', 'Monthly', 'Yearly')");
                    table.CheckConstraint("ck_transactions_type", "type IN ('Income', 'Expense')");
                    table.ForeignKey(
                        name: "fk_transactions_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_transactions_family_groups_family_group_id",
                        column: x => x.family_group_id,
                        principalTable: "family_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "shared_expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shared_expenses", x => x.id);
                    table.CheckConstraint("ck_shared_expenses_total_amount", "total_amount > 0");
                    table.ForeignKey(
                        name: "fk_shared_expenses_family_groups_family_group_id",
                        column: x => x.family_group_id,
                        principalTable: "family_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shared_expenses_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transaction_tags",
                columns: table => new
                {
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transaction_tags", x => new { x.transaction_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_transaction_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_transaction_tags_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shared_expense_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shared_expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_owed = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_settled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shared_expense_participants", x => x.id);
                    table.CheckConstraint("ck_shared_expense_participants_amount", "amount_owed > 0");
                    table.CheckConstraint("ck_shared_expense_participants_settled", "(is_settled = false AND settled_at IS NULL) OR (is_settled = true AND settled_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_shared_expense_participants_shared_expenses_shared_expense_",
                        column: x => x.shared_expense_id,
                        principalTable: "shared_expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_name_user_id",
                table: "categories",
                columns: new[] { "name", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_user_active",
                table: "categories",
                column: "user_id",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_family_groups_invite_code",
                table: "family_groups",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_family_members_group_user",
                table: "family_members",
                columns: new[] { "family_group_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shared_expense_participants_expense_user",
                table: "shared_expense_participants",
                columns: new[] { "shared_expense_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shared_expenses_group",
                table: "shared_expenses",
                column: "family_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_shared_expenses_transaction",
                table: "shared_expenses",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_user_name",
                table: "tags",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transaction_tags_tag_id",
                table: "transaction_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_category",
                table: "transactions",
                column: "category_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_family_group",
                table: "transactions",
                column: "family_group_id",
                filter: "family_group_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_date",
                table: "transactions",
                columns: new[] { "user_id", "transaction_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_type",
                table: "transactions",
                columns: new[] { "user_id", "type" },
                filter: "deleted_at IS NULL");

            // Triggers updated_at
            var tables = new[] { "categories", "tags", "transactions", "family_groups",
                                 "family_members", "shared_expenses", "shared_expense_participants" };

            foreach (var table in tables)
            {
                migrationBuilder.Sql($"""
                                      CREATE TRIGGER trg_{table}_updated_at
                                      BEFORE UPDATE ON {table}
                                      FOR EACH ROW
                                      EXECUTE FUNCTION set_updated_at();
                                      """);
            }

            // Full-text search em português
            migrationBuilder.Sql("""
                                 CREATE INDEX ix_transactions_fts
                                 ON transactions USING gin(to_tsvector('portuguese', description));
                                 """);
            }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove FTS index
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_transactions_fts;");

            // Remove triggers
            var tables = new[] { "shared_expense_participants", "shared_expenses",
                                 "family_members", "family_groups", "transactions", "tags", "categories" };

            foreach (var table in tables)
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS trg_{table}_updated_at ON {table};");
            }


            migrationBuilder.DropTable(
                name: "family_members");

            migrationBuilder.DropTable(
                name: "shared_expense_participants");

            migrationBuilder.DropTable(
                name: "transaction_tags");

            migrationBuilder.DropTable(
                name: "shared_expenses");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "family_groups");
        }
    }
}
