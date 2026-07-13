using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCheckInReplays : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "check_in_replays",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                event_id = table.Column<int>(type: "integer", nullable: false),
                client_scan_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                code_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                scanned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                accepted = table.Column<bool>(type: "boolean", nullable: false),
                response_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                rejection_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                ticket_id = table.Column<int>(type: "integer", nullable: true),
                checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_check_in_replays", x => x.id);
                table.ForeignKey(
                    name: "fk_check_in_replays_events_event_id",
                    column: x => x.event_id,
                    principalSchema: "app",
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_check_in_replays_tickets_ticket_id",
                    column: x => x.ticket_id,
                    principalSchema: "app",
                    principalTable: "tickets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_check_in_replays_ticket_id",
            schema: "app",
            table: "check_in_replays",
            column: "ticket_id");

        migrationBuilder.CreateIndex(
            name: "ux_check_in_replays_event_id_client_scan_id",
            schema: "app",
            table: "check_in_replays",
            columns: new[] { "event_id", "client_scan_id" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "check_in_replays",
            schema: "app");
    }
}
