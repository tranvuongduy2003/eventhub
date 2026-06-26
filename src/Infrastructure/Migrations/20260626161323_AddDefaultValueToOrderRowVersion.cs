using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDefaultValueToOrderRowVersion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<long>(
            name: "row_version",
            schema: "app",
            table: "orders",
            type: "bigint",
            rowVersion: true,
            nullable: false,
            defaultValue: 1L,
            oldClrType: typeof(long),
            oldType: "bigint",
            oldRowVersion: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<long>(
            name: "row_version",
            schema: "app",
            table: "orders",
            type: "bigint",
            rowVersion: true,
            nullable: false,
            oldClrType: typeof(long),
            oldType: "bigint",
            oldRowVersion: true,
            oldDefaultValue: 1L);
    }
}
