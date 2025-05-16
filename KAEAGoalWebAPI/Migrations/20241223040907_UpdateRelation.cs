using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KAEAGoalWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_USERS_USERSA_USER_ID",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_USERSA_USER_ID",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "USERSA_USER_ID",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_A_USER_ID",
                table: "RefreshTokens",
                column: "A_USER_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_USERS_A_USER_ID",
                table: "RefreshTokens",
                column: "A_USER_ID",
                principalTable: "USERS",
                principalColumn: "A_USER_ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_USERS_A_USER_ID",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_A_USER_ID",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<Guid>(
                name: "USERSA_USER_ID",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_USERSA_USER_ID",
                table: "RefreshTokens",
                column: "USERSA_USER_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_USERS_USERSA_USER_ID",
                table: "RefreshTokens",
                column: "USERSA_USER_ID",
                principalTable: "USERS",
                principalColumn: "A_USER_ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
