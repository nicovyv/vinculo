using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinculo.Migrations
{
    /// <inheritdoc />
    public partial class CrearObservaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Observaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonacionId = table.Column<int>(type: "int", nullable: true),
                    SolicitudId = table.Column<int>(type: "int", nullable: true),
                    AsignacionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observaciones_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Observaciones_Donaciones_DonacionId",
                        column: x => x.DonacionId,
                        principalTable: "Donaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Observaciones_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Observaciones_AsignacionId",
                table: "Observaciones",
                column: "AsignacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Observaciones_DonacionId",
                table: "Observaciones",
                column: "DonacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Observaciones_SolicitudId",
                table: "Observaciones",
                column: "SolicitudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Observaciones");
        }
    }
}
