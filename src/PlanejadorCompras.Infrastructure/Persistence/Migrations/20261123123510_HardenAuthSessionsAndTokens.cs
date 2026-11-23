using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanejadorCompras.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenAuthSessionsAndTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reset/confirmation tokens were stored in plain text. They are replaced in place by
            // their SHA-256 (lowercase hex of the UTF-8 bytes, the same value SecretToken.Hash
            // computes), so links already sent to users keep working and no raw token is left behind.
            migrationBuilder.DropIndex(
                name: "IX_UserTokens_Token",
                table: "UserTokens");

            migrationBuilder.Sql(
                """
                UPDATE "UserTokens"
                SET "Token" = encode(sha256(convert_to("Token", 'UTF8')), 'hex');
                """);

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "UserTokens",
                newName: "TokenHash");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "UserTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_TokenHash",
                table: "UserTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserTokens_TokenHash",
                table: "UserTokens");

            // A hash cannot be turned back into the token, and leaving hashes in a column the old
            // code compares as plain text would make each hash a usable token. Pending tokens are
            // discarded: users request a new reset link.
            migrationBuilder.Sql("""DELETE FROM "UserTokens";""");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "UserTokens",
                newName: "Token");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "UserTokens",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_Token",
                table: "UserTokens",
                column: "Token",
                unique: true);
        }
    }
}
