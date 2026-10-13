using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinculo.Migrations
{
    /// <inheritdoc />
    public partial class AgregaTablaDetalleDonacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "Donaciones");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Donaciones");

            migrationBuilder.CreateTable(
                name: "DetalleDonacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonacionId = table.Column<int>(type: "int", nullable: false),
                    TipoEquipamiento = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleDonacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalleDonacion_Donaciones_DonacionId",
                        column: x => x.DonacionId,
                        principalTable: "Donaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetalleDonacion_DonacionId",
                table: "DetalleDonacion",
                column: "DonacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetalleDonacion");

            migrationBuilder.AddColumn<int>(
                name: "Cantidad",
                table: "Donaciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Donaciones",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
