using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOccurrencesTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "occurrences",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                event_id = table.Column<int>(type: "integer", nullable: false),
                starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                venue_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_occurrences", x => x.id);
                table.ForeignKey(
                    name: "fk_occurrences_events_event_id",
                    column: x => x.event_id,
                    principalSchema: "app",
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_occurrences_event_id",
            schema: "app",
            table: "occurrences",
            column: "event_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "occurrences",
            schema: "app");
    }
}
