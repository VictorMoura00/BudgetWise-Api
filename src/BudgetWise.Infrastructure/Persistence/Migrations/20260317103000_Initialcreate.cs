using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetWise.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── PostgreSQL Extensions ──────────────────────────────────────────────
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS citext;");

        // ── Trigger function ───────────────────────────────────────────────────
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION set_updated_at()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.updated_at = now();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            """);

        // ── asp_net_roles ──────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "asp_net_roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                concurrency_stamp = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_roles", x => x.id);
            });

        // ── asp_net_users ──────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "asp_net_users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                user_name = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                normalized_user_name = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                email = table.Column<string>(type: "citext", maxLength: 256, nullable: false),
                normalized_email = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                password_hash = table.Column<string>(type: "text", nullable: true),
                security_stamp = table.Column<string>(type: "text", nullable: true),
                concurrency_stamp = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_users", x => x.id);
            });

        // ── asp_net_role_claims ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "asp_net_role_claims",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                claim_type = table.Column<string>(type: "text", nullable: true),
                claim_value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                table.ForeignKey(
                    name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "asp_net_roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ── asp_net_user_claims ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "asp_net_user_claims",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                claim_type = table.Column<string>(type: "text", nullable: true),
                claim_value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                table.ForeignKey(
                    name: "fk_asp_net_user_claims_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "asp_net_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ── asp_net_user_logins ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "asp_net_user_logins",
            columns: table => new
            {
                login_provider = table.Column<string>(type: "text", nullable: false),
                provider_key = table.Column<string>(type: "text", nullable: false),
                provider_display_name = table.Column<string>(type: "text", nullable: true),
                user_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                table.ForeignKey(
                    name: "fk_asp_net_user_logins_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "asp_net_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ── asp_net_user_roles ──────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "asp_net_user_roles",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                table.ForeignKey(
                    name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "asp_net_roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_asp_net_user_roles_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "asp_net_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ── asp_net_user_tokens ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "asp_net_user_tokens",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                login_provider = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                table.ForeignKey(
                    name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "asp_net_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ── Indexes ────────────────────────────────────────────────────────────
        migrationBuilder.CreateIndex(
            name: "ix_asp_net_role_claims_role_id",
            table: "asp_net_role_claims",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "role_name_index",
            table: "asp_net_roles",
            column: "normalized_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_asp_net_user_claims_user_id",
            table: "asp_net_user_claims",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_asp_net_user_logins_user_id",
            table: "asp_net_user_logins",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_asp_net_user_roles_role_id",
            table: "asp_net_user_roles",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "email_index",
            table: "asp_net_users",
            column: "normalized_email");

        migrationBuilder.CreateIndex(
            name: "user_name_index",
            table: "asp_net_users",
            column: "normalized_user_name",
            unique: true);

        // ── Trigger: updated_at on asp_net_users ───────────────────────────────
        migrationBuilder.Sql("""
            CREATE TRIGGER trg_asp_net_users_updated_at
            BEFORE UPDATE ON asp_net_users
            FOR EACH ROW
            EXECUTE FUNCTION set_updated_at();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "asp_net_role_claims");
        migrationBuilder.DropTable(name: "asp_net_user_claims");
        migrationBuilder.DropTable(name: "asp_net_user_logins");
        migrationBuilder.DropTable(name: "asp_net_user_roles");
        migrationBuilder.DropTable(name: "asp_net_user_tokens");
        migrationBuilder.DropTable(name: "asp_net_users");
        migrationBuilder.DropTable(name: "asp_net_roles");

        migrationBuilder.Sql("DROP FUNCTION IF EXISTS set_updated_at();");
        migrationBuilder.Sql("DROP EXTENSION IF EXISTS citext;");
        migrationBuilder.Sql("DROP EXTENSION IF EXISTS pgcrypto;");
    }
}