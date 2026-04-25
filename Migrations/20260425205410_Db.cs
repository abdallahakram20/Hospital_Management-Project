using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital_Management_Project.Migrations
{
    /// <inheritdoc />
    public partial class Db : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    DepartmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeptName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeptFloor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "Patient",
                columns: table => new
                {
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    user_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patient", x => x.PatientId);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    StaffID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fname = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Lname = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DeptID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.StaffID);
                    table.ForeignKey(
                        name: "FK_Staff_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId");
                });

            migrationBuilder.CreateTable(
                name: "PatientMedicalProfile",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Blood_Type = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Blood_Pressure = table.Column<bool>(type: "bit", nullable: false),
                    Chronic_Disease = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Allergies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Diabets = table.Column<bool>(type: "bit", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedicalProfile", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_PatientMedicalProfile_Patient_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointment",
                columns: table => new
                {
                    AppointmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Visit_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Medication = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Treatment_Plan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Common_tests = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PatientID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StaffID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointment", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "FK_Appointment_Patient_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patient",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointment_Staff_StaffID",
                        column: x => x.StaffID,
                        principalTable: "Staff",
                        principalColumn: "StaffID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_PatientID",
                table: "Appointment",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_StaffID",
                table: "Appointment",
                column: "StaffID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalProfile_PatientId",
                table: "PatientMedicalProfile",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_DepartmentId",
                table: "Staff",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointment");

            migrationBuilder.DropTable(
                name: "PatientMedicalProfile");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "Patient");

            migrationBuilder.DropTable(
                name: "Department");
        }
    }
}
