using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSalesWindowToTicketTypes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "sales_window_end",
            schema: "app",
            table: "ticket_types",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "sales_window_start",
            schema: "app",
            table: "ticket_types",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "sales_window_end",
            schema: "app",
            table: "ticket_types");

        migrationBuilder.DropColumn(
            name: "sales_window_start",
            schema: "app",
            table: "ticket_types");
    }
}
