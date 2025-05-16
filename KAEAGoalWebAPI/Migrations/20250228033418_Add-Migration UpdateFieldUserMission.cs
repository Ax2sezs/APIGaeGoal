using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddMigrationUpdateFieldUserMission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReward",
                table: "USER_TEXT_MISSIONS",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Approve_At",
                table: "USER_QR_CODE_MISSIONS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Approve_At",
                table: "USER_PHOTO_MISSIONS",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReward",
                table: "USER_TEXT_MISSIONS");

            migrationBuilder.DropColumn(
                name: "Approve_At",
                table: "USER_QR_CODE_MISSIONS");

            migrationBuilder.DropColumn(
                name: "Approve_At",
                table: "USER_PHOTO_MISSIONS");
        }
    }
}
