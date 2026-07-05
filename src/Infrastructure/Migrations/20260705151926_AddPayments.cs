using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPayments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "payments",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                order_id = table.Column<int>(type: "integer", nullable: false),
                amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                initiated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                refunded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_payments", x => x.id);
                table.ForeignKey(
                    name: "fk_payments_orders_order_id",
                    column: x => x.order_id,
                    principalSchema: "app",
                    principalTable: "orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_payments_order_id",
            schema: "app",
            table: "payments",
            column: "order_id");

        migrationBuilder.CreateIndex(
            name: "ux_payments_provider_reference",
            schema: "app",
            table: "payments",
            column: "provider_reference",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "payments",
            schema: "app");
    }
}
