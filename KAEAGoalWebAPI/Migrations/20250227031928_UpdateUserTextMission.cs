using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class UpdateUserTextMission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Submitted_At",
                table: "USER_MISSIONS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "USER_TEXT_MISSIONS",
                columns: table => new
                {
                    USER_TEXT_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    USER_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    USER_TEXT = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Submitted_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Approve = table.Column<bool>(type: "bit", nullable: true),
                    Approve_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Approve_At = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_TEXT_MISSIONS", x => x.USER_TEXT_MISSION_ID);
                    table.ForeignKey(
                        name: "FK_USER_TEXT_MISSIONS_MISSIONS_MISSION_ID",
                        column: x => x.MISSION_ID,
                        principalTable: "MISSIONS",
                        principalColumn: "MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_TEXT_MISSIONS_USER_MISSIONS_USER_MISSION_ID",
                        column: x => x.USER_MISSION_ID,
                        principalTable: "USER_MISSIONS",
                        principalColumn: "USER_MISSION_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_TEXT_MISSIONS_USERS_A_USER_ID",
                        column: x => x.A_USER_ID,
                        principalTable: "USERS",
                        principalColumn: "A_USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_TEXT_MISSIONS_A_USER_ID",
                table: "USER_TEXT_MISSIONS",
                column: "A_USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_TEXT_MISSIONS_MISSION_ID",
                table: "USER_TEXT_MISSIONS",
                column: "MISSION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_TEXT_MISSIONS_USER_MISSION_ID",
                table: "USER_TEXT_MISSIONS",
                column: "USER_MISSION_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_TEXT_MISSIONS");

            migrationBuilder.DropColumn(
                name: "Submitted_At",
                table: "USER_MISSIONS");
        }
    }
}
