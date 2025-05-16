using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KAEAGoalWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeMissionID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CODE_MISSIONS",
                table: "CODE_MISSIONS");

            migrationBuilder.AddColumn<Guid>(
                name: "CodeMissionID",
                table: "CODE_MISSIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_CODE_MISSIONS",
                table: "CODE_MISSIONS",
                column: "CodeMissionID");

            migrationBuilder.CreateIndex(
                name: "IX_CODE_MISSIONS_MISSION_ID",
                table: "CODE_MISSIONS",
                column: "MISSION_ID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CODE_MISSIONS",
                table: "CODE_MISSIONS");

            migrationBuilder.DropIndex(
                name: "IX_CODE_MISSIONS_MISSION_ID",
                table: "CODE_MISSIONS");

            migrationBuilder.DropColumn(
                name: "CodeMissionID",
                table: "CODE_MISSIONS");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CODE_MISSIONS",
                table: "CODE_MISSIONS",
                column: "MISSION_ID");
        }
    }
}
