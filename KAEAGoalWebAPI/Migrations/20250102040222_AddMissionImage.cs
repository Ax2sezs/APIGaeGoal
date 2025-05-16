using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KAEAGoalWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MISSION_IMAGES",
                columns: table => new
                {
                    IMAGE_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uploaded_Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MISSION_IMAGES", x => x.IMAGE_ID);
                    table.ForeignKey(
                        name: "FK_MISSION_IMAGES_MISSIONS_MISSION_ID",
                        column: x => x.MISSION_ID,
                        principalTable: "MISSIONS",
                        principalColumn: "MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MISSION_IMAGES_MISSION_ID",
                table: "MISSION_IMAGES",
                column: "MISSION_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MISSION_IMAGES");
        }
    }
}
