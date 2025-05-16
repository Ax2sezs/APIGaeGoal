using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddQRCodeMission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QR_CODE_MISSIONS",
                columns: table => new
                {
                    QR_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QRCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QR_CODE_MISSIONS", x => x.QR_MISSION_ID);
                    table.ForeignKey(
                        name: "FK_QR_CODE_MISSIONS_MISSIONS_MISSION_ID",
                        column: x => x.MISSION_ID,
                        principalTable: "MISSIONS",
                        principalColumn: "MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QR_CODE_MISSIONS_MISSION_ID",
                table: "QR_CODE_MISSIONS",
                column: "MISSION_ID",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QR_CODE_MISSIONS");
        }
    }
}
