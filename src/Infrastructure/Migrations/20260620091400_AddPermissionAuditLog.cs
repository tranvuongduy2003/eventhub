using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPermissionAuditLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "permission_audit_log",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                event_id = table.Column<int>(type: "integer", nullable: false),
                actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                target_id = table.Column<Guid>(type: "uuid", nullable: false),
                action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                old_role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                new_role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_permission_audit_log", x => x.id);
                table.ForeignKey(
                    name: "FK_permission_audit_log_users_actor_id",
                    column: x => x.actor_id,
                    principalSchema: "app",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_permission_audit_log_users_target_id",
                    column: x => x.target_id,
                    principalSchema: "app",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_permission_audit_log_actor_id",
            schema: "app",
            table: "permission_audit_log",
            column: "actor_id");

        migrationBuilder.CreateIndex(
            name: "ix_permission_audit_log_event_id_occurred_at",
            schema: "app",
            table: "permission_audit_log",
            columns: new[] { "event_id", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "IX_permission_audit_log_target_id",
            schema: "app",
            table: "permission_audit_log",
            column: "target_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "permission_audit_log",
            schema: "app");
    }
}
