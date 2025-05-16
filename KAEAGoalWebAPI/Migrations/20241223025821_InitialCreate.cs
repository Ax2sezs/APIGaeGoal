using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KAEAGoalWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    A_USER_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LOGON_NAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    USER_PASSWORD = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BranchCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Branch = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StateCode = table.Column<int>(type: "int", nullable: false),
                    DeletionStateCode = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsBkk = table.Column<int>(type: "int", nullable: true),
                    IsAdmin = table.Column<int>(type: "int", nullable: true),
                    User_Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Isshop = table.Column<bool>(type: "bit", nullable: false),
                    Issup = table.Column<bool>(type: "bit", nullable: false),
                    ST_Dept_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsQSC = table.Column<int>(type: "int", nullable: true),
                    USER_EMAIL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    User_Position = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.A_USER_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USERS");
        }
    }
}
