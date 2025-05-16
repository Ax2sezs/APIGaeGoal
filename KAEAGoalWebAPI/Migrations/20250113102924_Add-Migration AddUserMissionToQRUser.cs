using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddMigrationAddUserMissionToQRUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "USER_MISSION_ID",
                table: "USER_QR_CODE_MISSIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_USER_QR_CODE_MISSIONS_USER_MISSION_ID",
                table: "USER_QR_CODE_MISSIONS",
                column: "USER_MISSION_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_USER_QR_CODE_MISSIONS_USER_MISSIONS_USER_MISSION_ID",
                table: "USER_QR_CODE_MISSIONS",
                column: "USER_MISSION_ID",
                principalTable: "USER_MISSIONS",
                principalColumn: "USER_MISSION_ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USER_QR_CODE_MISSIONS_USER_MISSIONS_USER_MISSION_ID",
                table: "USER_QR_CODE_MISSIONS");

            migrationBuilder.DropIndex(
                name: "IX_USER_QR_CODE_MISSIONS_USER_MISSION_ID",
                table: "USER_QR_CODE_MISSIONS");

            migrationBuilder.DropColumn(
                name: "USER_MISSION_ID",
                table: "USER_QR_CODE_MISSIONS");
        }
    }
}
