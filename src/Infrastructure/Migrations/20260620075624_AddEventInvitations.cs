using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEventInvitations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "event_invitation",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                event_id = table.Column<int>(type: "integer", nullable: false),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                token_hash = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                inviter_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_event_invitation", x => x.id);
                table.ForeignKey(
                    name: "FK_event_invitation_users_inviter_id",
                    column: x => x.inviter_id,
                    principalSchema: "app",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_event_invitation_event_id",
            schema: "app",
            table: "event_invitation",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_event_invitation_event_id_email_status",
            schema: "app",
            table: "event_invitation",
            columns: new[] { "event_id", "email", "status" });

        migrationBuilder.CreateIndex(
            name: "IX_event_invitation_inviter_id",
            schema: "app",
            table: "event_invitation",
            column: "inviter_id");

        migrationBuilder.CreateIndex(
            name: "ux_event_invitation_pending_email",
            schema: "app",
            table: "event_invitation",
            columns: new[] { "event_id", "email" },
            unique: true,
            filter: "status = 'Pending'");

        migrationBuilder.CreateIndex(
            name: "ux_event_invitation_token_hash",
            schema: "app",
            table: "event_invitation",
            column: "token_hash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "event_invitation",
            schema: "app");
    }
}
