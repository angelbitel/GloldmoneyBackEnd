using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldmoneyBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPinUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsuariosPin",
                columns: table => new
                {
                    NombreUsuario = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    PinHash = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosPin", x => x.NombreUsuario);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuariosPin");
        }
    }
}
