using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddTickets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tickets",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                event_id = table.Column<int>(type: "integer", nullable: false),
                order_id = table.Column<int>(type: "integer", nullable: false),
                ticket_type_id = table.Column<int>(type: "integer", nullable: false),
                code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                holder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                holder_email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tickets", x => x.id);
                table.ForeignKey(
                    name: "fk_tickets_events_event_id",
                    column: x => x.event_id,
                    principalSchema: "app",
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_tickets_orders_order_id",
                    column: x => x.order_id,
                    principalSchema: "app",
                    principalTable: "orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_tickets_ticket_types_ticket_type_id",
                    column: x => x.ticket_type_id,
                    principalSchema: "app",
                    principalTable: "ticket_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_tickets_event_id",
            schema: "app",
            table: "tickets",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_tickets_holder_email",
            schema: "app",
            table: "tickets",
            column: "holder_email");

        migrationBuilder.CreateIndex(
            name: "ix_tickets_order_id",
            schema: "app",
            table: "tickets",
            column: "order_id");

        migrationBuilder.CreateIndex(
            name: "IX_tickets_ticket_type_id",
            schema: "app",
            table: "tickets",
            column: "ticket_type_id");

        migrationBuilder.CreateIndex(
            name: "ux_tickets_code",
            schema: "app",
            table: "tickets",
            column: "code",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "tickets",
            schema: "app");
    }
}
