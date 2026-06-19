using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEventsTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "events",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                organizer_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                schedule_starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                schedule_ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                schedule_time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                location_physical_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                location_is_online = table.Column<bool>(type: "boolean", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_events", x => x.id);
                table.ForeignKey(
                    name: "fk_events_users_organizer_id",
                    column: x => x.organizer_id,
                    principalSchema: "app",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_events_organizer_id",
            schema: "app",
            table: "events",
            column: "organizer_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "events",
            schema: "app");
    }
}
