using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldmoneyBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSoporteTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoEmpresa = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Prioridad = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Origen = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsuarioAsignado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AsignadoA = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UltimaRespuestaDe = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    NumeroContrato = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Pantalla = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCierreUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketMensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Autor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EsSoporte = table.Column<bool>(type: "bit", nullable: false),
                    EsInterno = table.Column<bool>(type: "bit", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMensajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketMensajes_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketAdjuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MensajeId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Tamano = table.Column<long>(type: "bigint", nullable: false),
                    Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAdjuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketAdjuntos_TicketMensajes_MensajeId",
                        column: x => x.MensajeId,
                        principalTable: "TicketMensajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketAdjuntos_MensajeId",
                table: "TicketAdjuntos",
                column: "MensajeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMensajes_TicketId",
                table: "TicketMensajes",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CodigoEmpresa_FechaActualizacionUtc",
                table: "Tickets",
                columns: new[] { "CodigoEmpresa", "FechaActualizacionUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Estado_FechaActualizacionUtc",
                table: "Tickets",
                columns: new[] { "Estado", "FechaActualizacionUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketAdjuntos");

            migrationBuilder.DropTable(
                name: "TicketMensajes");

            migrationBuilder.DropTable(
                name: "Tickets");
        }
    }
}
