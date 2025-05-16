using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class UpdateAndAddVideo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReward",
                table: "USER_QR_CODE_MISSIONS",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReward",
                table: "USER_PHOTO_MISSIONS",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Winners",
                table: "MISSIONS",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "USER_VIDEO_MISSIONS",
                columns: table => new
                {
                    USER_VIDEO_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    USER_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uploaded_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Approve = table.Column<bool>(type: "bit", nullable: true),
                    Approved_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    isReward = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_VIDEO_MISSIONS", x => x.USER_VIDEO_MISSION_ID);
                    table.ForeignKey(
                        name: "FK_USER_VIDEO_MISSIONS_MISSIONS_MISSION_ID",
                        column: x => x.MISSION_ID,
                        principalTable: "MISSIONS",
                        principalColumn: "MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_VIDEO_MISSIONS_USER_MISSIONS_USER_MISSION_ID",
                        column: x => x.USER_MISSION_ID,
                        principalTable: "USER_MISSIONS",
                        principalColumn: "USER_MISSION_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_VIDEO_MISSIONS_USERS_A_USER_ID",
                        column: x => x.A_USER_ID,
                        principalTable: "USERS",
                        principalColumn: "A_USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_VIDEO_MISSIONS_A_USER_ID",
                table: "USER_VIDEO_MISSIONS",
                column: "A_USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_VIDEO_MISSIONS_MISSION_ID",
                table: "USER_VIDEO_MISSIONS",
                column: "MISSION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_VIDEO_MISSIONS_USER_MISSION_ID",
                table: "USER_VIDEO_MISSIONS",
                column: "USER_MISSION_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_VIDEO_MISSIONS");

            migrationBuilder.DropColumn(
                name: "IsReward",
                table: "USER_QR_CODE_MISSIONS");

            migrationBuilder.DropColumn(
                name: "IsReward",
                table: "USER_PHOTO_MISSIONS");

            migrationBuilder.DropColumn(
                name: "Winners",
                table: "MISSIONS");
        }
    }
}
