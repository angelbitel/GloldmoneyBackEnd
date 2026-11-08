using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldmoneyBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFormaPagoCobros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cobros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IdCliente = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: false),
                    TotalMetodos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDocumentos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cobros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CobrosDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CobroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CodigoEmpresa = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    CodigoGrupo = table.Column<int>(type: "int", nullable: false),
                    NumeroContrato = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    NumeroRecibo = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CobrosDocumento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CobrosDocumento_Cobros_CobroId",
                        column: x => x.CobroId,
                        principalTable: "Cobros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CobrosMetodoPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CobroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Metodo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CobrosMetodoPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CobrosMetodoPago_Cobros_CobroId",
                        column: x => x.CobroId,
                        principalTable: "Cobros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cobros_FechaUtc",
                table: "Cobros",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Cobros_IdCliente",
                table: "Cobros",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_CobrosDocumento_CobroId",
                table: "CobrosDocumento",
                column: "CobroId");

            migrationBuilder.CreateIndex(
                name: "IX_CobrosDocumento_CodigoEmpresa_Tipo_NumeroRecibo",
                table: "CobrosDocumento",
                columns: new[] { "CodigoEmpresa", "Tipo", "NumeroRecibo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CobrosMetodoPago_CobroId_Metodo",
                table: "CobrosMetodoPago",
                columns: new[] { "CobroId", "Metodo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CobrosDocumento");

            migrationBuilder.DropTable(
                name: "CobrosMetodoPago");

            migrationBuilder.DropTable(
                name: "Cobros");
        }
    }
}
