using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace main_server_api.Migrations;

/// <inheritdoc />
public partial class dev1 : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "Devices",
			columns: table => new {
				Id = table.Column<long>(type: "bigint", nullable: false)
					.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
				UserId = table.Column<int>(type: "integer", nullable: false),
				WireguardPublicKey = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
				LastConnectedNodeId = table.Column<int>(type: "integer", nullable: true),
				LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
			},
			constraints: table => {
				table.PrimaryKey("PK_Devices", x => x.Id);
				table.UniqueConstraint("AK_Devices_WireguardPublicKey", x => x.WireguardPublicKey);
			});

		migrationBuilder.CreateTable(
			name: "Users",
			columns: table => new {
				Id = table.Column<int>(type: "integer", nullable: false)
					.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
				IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
				Email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
				IsEmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
				PasswordSalt = table.Column<byte[]>(type: "bytea", maxLength: 64, nullable: false),
				PasswordHash = table.Column<byte[]>(type: "bytea", maxLength: 64, nullable: false),
				PayedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
				RefreshTokensEntropies = table.Column<List<long>>(type: "bigint[]", nullable: false)
			},
			constraints: table => {
				table.PrimaryKey("PK_Users", x => x.Id);
				table.UniqueConstraint("AK_Users_Email", x => x.Email);
			});
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(
			name: "Devices");

		migrationBuilder.DropTable(
			name: "Users");
	}
}
