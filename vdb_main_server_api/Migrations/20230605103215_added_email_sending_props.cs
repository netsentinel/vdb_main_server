using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace main_server_api.Migrations
{
    /// <inheritdoc />
    public partial class added_email_sending_props : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSendedEmail",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "RecoveryJwtEntropy",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSendedEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RecoveryJwtEntropy",
                table: "Users");
        }
    }
}
