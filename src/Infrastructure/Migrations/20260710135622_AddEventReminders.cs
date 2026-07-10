using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEventReminders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "event_reminders",
            schema: "app",
            columns: table => new
            {
                event_id = table.Column<int>(type: "integer", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false),
                lead_time_minutes = table.Column<int>(type: "integer", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_event_reminders", x => x.event_id);
                table.ForeignKey(
                    name: "fk_event_reminders_events_event_id",
                    column: x => x.event_id,
                    principalSchema: "app",
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "event_reminders",
            schema: "app");
    }
}
