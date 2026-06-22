using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEventSlug : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "slug",
            schema: "app",
            table: "events",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_events_slug",
            schema: "app",
            table: "events",
            column: "slug",
            unique: true,
            filter: "slug IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_events_slug",
            schema: "app",
            table: "events");

        migrationBuilder.DropColumn(
            name: "slug",
            schema: "app",
            table: "events");
    }
}
