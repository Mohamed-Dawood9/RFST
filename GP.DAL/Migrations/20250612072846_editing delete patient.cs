using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.DAL.Migrations
{
    public partial class editingdeletepatient : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Analyses_AnalysisId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Appointments_AppointmentId",
                table: "Notes");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Analyses_AnalysisId",
                table: "Notes",
                column: "AnalysisId",
                principalTable: "Analyses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Appointments_AppointmentId",
                table: "Notes",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Analyses_AnalysisId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Appointments_AppointmentId",
                table: "Notes");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Analyses_AnalysisId",
                table: "Notes",
                column: "AnalysisId",
                principalTable: "Analyses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Appointments_AppointmentId",
                table: "Notes",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }
    }
}
