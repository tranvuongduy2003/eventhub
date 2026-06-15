using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class ReplaceUsernameWithDisplayNameAndRole : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_users_username",
            schema: "app",
            table: "users");

        migrationBuilder.RenameColumn(
            name: "username",
            schema: "app",
            table: "users",
            newName: "display_name");

        migrationBuilder.AlterColumn<string>(
            name: "display_name",
            schema: "app",
            table: "users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32);

        migrationBuilder.AddColumn<string>(
            name: "role",
            schema: "app",
            table: "users",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Organizer");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "role",
            schema: "app",
            table: "users");

        migrationBuilder.AlterColumn<string>(
            name: "display_name",
            schema: "app",
            table: "users",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64);

        migrationBuilder.RenameColumn(
            name: "display_name",
            schema: "app",
            table: "users",
            newName: "username");

        migrationBuilder.CreateIndex(
            name: "ux_users_username",
            schema: "app",
            table: "users",
            column: "username",
            unique: true);
    }
}
