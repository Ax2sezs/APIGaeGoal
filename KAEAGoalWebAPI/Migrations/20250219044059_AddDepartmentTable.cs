using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KAEAGoalWebAPI.Migrations
{
    public partial class AddDepartmentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DEPARTMENTS",
                columns: table => new
                {
                    DEPT_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DEPT_NAME = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEPARTMENTS", x => x.DEPT_ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DEPARTMENTS");
        }
    }
}
