using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOrders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "orders",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                event_id = table.Column<int>(type: "integer", nullable: false),
                contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                contact_email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                total_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                total_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                payment_id = table.Column<int>(type: "integer", nullable: true),
                placed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_orders", x => x.id);
                table.ForeignKey(
                    name: "fk_orders_events_event_id",
                    column: x => x.event_id,
                    principalSchema: "app",
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "order_lines",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                order_id = table.Column<int>(type: "integer", nullable: false),
                ticket_type_id = table.Column<int>(type: "integer", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                unit_price_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                line_total_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                line_total_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_order_lines", x => x.id);
                table.ForeignKey(
                    name: "fk_order_lines_orders_order_id",
                    column: x => x.order_id,
                    principalSchema: "app",
                    principalTable: "orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_order_lines_ticket_types_ticket_type_id",
                    column: x => x.ticket_type_id,
                    principalSchema: "app",
                    principalTable: "ticket_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_order_lines_order_id",
            schema: "app",
            table: "order_lines",
            column: "order_id");

        migrationBuilder.CreateIndex(
            name: "IX_order_lines_ticket_type_id",
            schema: "app",
            table: "order_lines",
            column: "ticket_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_orders_event_id",
            schema: "app",
            table: "orders",
            column: "event_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "order_lines",
            schema: "app");

        migrationBuilder.DropTable(
            name: "orders",
            schema: "app");
    }
}
