using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoatVaultInfrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddArgon2ParametersToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    _id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    AuthSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    AuthVerifier = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Argon2TimeCost = table.Column<int>(type: "INTEGER", nullable: false),
                    Argon2MemoryCost = table.Column<int>(type: "INTEGER", nullable: false),
                    Argon2Lanes = table.Column<int>(type: "INTEGER", nullable: false),
                    Argon2Threads = table.Column<int>(type: "INTEGER", nullable: false),
                    Argon2HashLength = table.Column<int>(type: "INTEGER", nullable: false),
                    MfaEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MfaSecret = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ShamirEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    VaultSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    VaultEncryptedBlob = table.Column<byte[]>(type: "BLOB", nullable: false),
                    VaultNonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    VaultAuthTag = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x._id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
