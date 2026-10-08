using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinculo.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateDonacionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Donaciones_DonacionId",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Donaciones_DonacionId1",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Solicitudes_SolicitudId",
                table: "Asignaciones");

            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_DonacionId1",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "DonacionId1",
                table: "Asignaciones");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Donaciones_DonacionId",
                table: "Asignaciones",
                column: "DonacionId",
                principalTable: "Donaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Solicitudes_SolicitudId",
                table: "Asignaciones",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Donaciones_DonacionId",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Solicitudes_SolicitudId",
                table: "Asignaciones");

            migrationBuilder.AddColumn<int>(
                name: "DonacionId1",
                table: "Asignaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_DonacionId1",
                table: "Asignaciones",
                column: "DonacionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Donaciones_DonacionId",
                table: "Asignaciones",
                column: "DonacionId",
                principalTable: "Donaciones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Donaciones_DonacionId1",
                table: "Asignaciones",
                column: "DonacionId1",
                principalTable: "Donaciones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Solicitudes_SolicitudId",
                table: "Asignaciones",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id");
        }
    }
}
