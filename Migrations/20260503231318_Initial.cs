using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital_Management_Project.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Department_DepartmentId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "Staff");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Staff",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Department_DepartmentId",
                table: "Staff",
                column: "DepartmentId",
                principalTable: "Department",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Department_DepartmentId",
                table: "Staff");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Staff",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "Staff",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Department_DepartmentId",
                table: "Staff",
                column: "DepartmentId",
                principalTable: "Department",
                principalColumn: "DepartmentId");
        }
    }
}
