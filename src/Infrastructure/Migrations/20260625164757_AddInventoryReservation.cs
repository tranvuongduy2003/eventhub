using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddInventoryReservation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "reservation_id",
            schema: "app",
            table: "orders",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "reservations",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                event_id = table.Column<int>(type: "integer", nullable: false),
                ticket_type_id = table.Column<int>(type: "integer", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                order_id = table.Column<int>(type: "integer", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reservations", x => x.id);
                table.ForeignKey(
                    name: "fk_reservations_events_event_id",
                    column: x => x.event_id,
                    principalSchema: "app",
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_reservations_orders_order_id",
                    column: x => x.order_id,
                    principalSchema: "app",
                    principalTable: "orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_reservations_event_id",
            schema: "app",
            table: "reservations",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_reservations_expires_at",
            schema: "app",
            table: "reservations",
            column: "expires_at");

        migrationBuilder.CreateIndex(
            name: "ix_reservations_order_id",
            schema: "app",
            table: "reservations",
            column: "order_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "reservations",
            schema: "app");

        migrationBuilder.DropColumn(
            name: "reservation_id",
            schema: "app",
            table: "orders");
    }
}
