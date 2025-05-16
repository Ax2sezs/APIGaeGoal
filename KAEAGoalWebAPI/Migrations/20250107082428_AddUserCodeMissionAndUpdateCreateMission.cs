using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KAEAGoalWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCodeMissionAndUpdateCreateMission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Created_At",
                table: "MISSIONS",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "USER_CODE_MISSIONS",
                columns: table => new
                {
                    USER_CODE_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Submit_At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_CODE_MISSIONS", x => x.USER_CODE_MISSION_ID);
                    table.ForeignKey(
                        name: "FK_USER_CODE_MISSIONS_MISSIONS_MISSION_ID",
                        column: x => x.MISSION_ID,
                        principalTable: "MISSIONS",
                        principalColumn: "MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_CODE_MISSIONS_USERS_A_USER_ID",
                        column: x => x.A_USER_ID,
                        principalTable: "USERS",
                        principalColumn: "A_USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_CODE_MISSIONS_A_USER_ID",
                table: "USER_CODE_MISSIONS",
                column: "A_USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_CODE_MISSIONS_MISSION_ID",
                table: "USER_CODE_MISSIONS",
                column: "MISSION_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_CODE_MISSIONS");

            migrationBuilder.DropColumn(
                name: "Created_At",
                table: "MISSIONS");
        }
    }
}
