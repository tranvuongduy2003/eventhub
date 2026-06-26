using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDiscountCodes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "discount_amount",
            schema: "app",
            table: "orders",
            type: "numeric(12,2)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "discount_code_id",
            schema: "app",
            table: "orders",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "discount_codes",
            schema: "app",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                event_id = table.Column<int>(type: "integer", nullable: false),
                code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                usage_cap = table.Column<int>(type: "integer", nullable: true),
                used_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_discount_codes", x => x.id);
                table.ForeignKey(
                    name: "fk_discount_codes_events_event_id",
                    column: x => x.event_id,
                    principalSchema: "app",
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_discount_codes_event_id",
            schema: "app",
            table: "discount_codes",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_discount_codes_event_id_code",
            schema: "app",
            table: "discount_codes",
            columns: new[] { "event_id", "code" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "discount_codes",
            schema: "app");

        migrationBuilder.DropColumn(
            name: "discount_amount",
            schema: "app",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "discount_code_id",
            schema: "app",
            table: "orders");
    }
}
