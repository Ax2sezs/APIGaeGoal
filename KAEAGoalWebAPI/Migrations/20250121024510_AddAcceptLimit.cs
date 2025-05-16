using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddAcceptLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Accept_limit",
                table: "MISSIONS",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Current_Accept",
                table: "MISSIONS",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Accept_limit",
                table: "MISSIONS");

            migrationBuilder.DropColumn(
                name: "Current_Accept",
                table: "MISSIONS");
        }
    }
}
