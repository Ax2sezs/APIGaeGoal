using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddNewQRUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USER_QR_CODE_MISSIONS",
                columns: table => new
                {
                    USER_QRCODE_MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QRCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MISSION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scanned_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Approve = table.Column<bool>(type: "bit", nullable: true),
                    Approved_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_QR_CODE_MISSIONS", x => x.USER_QRCODE_MISSION_ID);
                    table.ForeignKey(
                        name: "FK_USER_QR_CODE_MISSIONS_MISSIONS_MISSION_ID",
                        column: x => x.MISSION_ID,
                        principalTable: "MISSIONS",
                        principalColumn: "MISSION_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_QR_CODE_MISSIONS_USERS_A_USER_ID",
                        column: x => x.A_USER_ID,
                        principalTable: "USERS",
                        principalColumn: "A_USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_QR_CODE_MISSIONS_A_USER_ID",
                table: "USER_QR_CODE_MISSIONS",
                column: "A_USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_QR_CODE_MISSIONS_MISSION_ID",
                table: "USER_QR_CODE_MISSIONS",
                column: "MISSION_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_QR_CODE_MISSIONS");
        }
    }
}
