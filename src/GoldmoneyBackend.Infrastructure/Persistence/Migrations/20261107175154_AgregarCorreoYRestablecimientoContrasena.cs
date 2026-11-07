using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldmoneyBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCorreoYRestablecimientoContrasena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestablecimientosContrasena",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreUsuario = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiraEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestablecimientosContrasena", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosCorreo",
                columns: table => new
                {
                    NombreUsuario = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosCorreo", x => x.NombreUsuario);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestablecimientosContrasena_NombreUsuario_CreadoEnUtc",
                table: "RestablecimientosContrasena",
                columns: new[] { "NombreUsuario", "CreadoEnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RestablecimientosContrasena_TokenHash",
                table: "RestablecimientosContrasena",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosCorreo_Correo",
                table: "UsuariosCorreo",
                column: "Correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestablecimientosContrasena");

            migrationBuilder.DropTable(
                name: "UsuariosCorreo");
        }
    }
}
