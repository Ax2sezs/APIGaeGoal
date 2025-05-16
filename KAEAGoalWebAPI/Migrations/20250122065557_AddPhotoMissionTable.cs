using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddPhotoMissionTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USER_PHOTO_MISSIONS",
                columns: table => new
                {
                    USER_PHOTO_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    USER_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Uploaded_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Approve = table.Column<bool>(type: "bit", nullable: true),
                    Approve_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_PHOTO_MISSIONS", x => x.USER_PHOTO_MISSION_ID);
                    table.ForeignKey(
                        name: "FK_USER_PHOTO_MISSIONS_MISSIONS_MISSION_ID",
                        column: x => x.MISSION_ID,
                        principalTable: "MISSIONS",
                        principalColumn: "MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_PHOTO_MISSIONS_USER_MISSIONS_USER_MISSION_ID",
                        column: x => x.USER_MISSION_ID,
                        principalTable: "USER_MISSIONS",
                        principalColumn: "USER_MISSION_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_PHOTO_MISSIONS_USERS_A_USER_ID",
                        column: x => x.A_USER_ID,
                        principalTable: "USERS",
                        principalColumn: "A_USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_PHOTO_MISSION_IMAGES",
                columns: table => new
                {
                    USER_PHOTO_MISSION_IMAGE_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    USER_PHOTO_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_PHOTO_MISSION_IMAGES", x => x.USER_PHOTO_MISSION_IMAGE_ID);
                    table.ForeignKey(
                        name: "FK_USER_PHOTO_MISSION_IMAGES_USER_PHOTO_MISSIONS_USER_PHOTO_MISSION_ID",
                        column: x => x.USER_PHOTO_MISSION_ID,
                        principalTable: "USER_PHOTO_MISSIONS",
                        principalColumn: "USER_PHOTO_MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_PHOTO_MISSION_IMAGES_USER_PHOTO_MISSION_ID",
                table: "USER_PHOTO_MISSION_IMAGES",
                column: "USER_PHOTO_MISSION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_PHOTO_MISSIONS_A_USER_ID",
                table: "USER_PHOTO_MISSIONS",
                column: "A_USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_PHOTO_MISSIONS_MISSION_ID",
                table: "USER_PHOTO_MISSIONS",
                column: "MISSION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_PHOTO_MISSIONS_USER_MISSION_ID",
                table: "USER_PHOTO_MISSIONS",
                column: "USER_MISSION_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_PHOTO_MISSION_IMAGES");

            migrationBuilder.DropTable(
                name: "USER_PHOTO_MISSIONS");
        }
    }
}
