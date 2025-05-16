using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddParticipate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Site",
                table: "USERS",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isForcePassChange",
                table: "USERS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isRegister",
                table: "USERS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "Missioner",
                table: "MISSIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Participate_Type",
                table: "MISSIONS",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Site",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "isForcePassChange",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "isRegister",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "Missioner",
                table: "MISSIONS");

            migrationBuilder.DropColumn(
                name: "Participate_Type",
                table: "MISSIONS");
        }
    }
}
