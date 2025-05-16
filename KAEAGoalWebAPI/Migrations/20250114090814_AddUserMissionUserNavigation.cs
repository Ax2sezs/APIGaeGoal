using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddUserMissionUserNavigation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_USER_MISSIONS_A_USER_ID",
                table: "USER_MISSIONS",
                column: "A_USER_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_USER_MISSIONS_USERS_A_USER_ID",
                table: "USER_MISSIONS",
                column: "A_USER_ID",
                principalTable: "USERS",
                principalColumn: "A_USER_ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USER_MISSIONS_USERS_A_USER_ID",
                table: "USER_MISSIONS");

            migrationBuilder.DropIndex(
                name: "IX_USER_MISSIONS_A_USER_ID",
                table: "USER_MISSIONS");
        }
    }
}
