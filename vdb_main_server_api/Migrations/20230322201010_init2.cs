using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace main_server_api.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "UserDevices");

            migrationBuilder.AlterColumn<List<long>>(
                name: "UserDevicesIds",
                table: "Users",
                type: "bigint[]",
                nullable: false,
                oldClrType: typeof(List<int>),
                oldType: "integer[]");

            migrationBuilder.AlterColumn<int>(
                name: "LastConnectedNodeId",
                table: "UserDevices",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<int>>(
                name: "UserDevicesIds",
                table: "Users",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(List<long>),
                oldType: "bigint[]");

            migrationBuilder.AlterColumn<int>(
                name: "LastConnectedNodeId",
                table: "UserDevices",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "UserDevices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
