using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolicitudIT_Isertec.Migrations
{
    /// <inheritdoc />
    public partial class RequestedAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mensajes_privados");

            migrationBuilder.AddColumn<string>(
                name: "anydesk_id",
                table: "solicitudes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "inventario_mantenimiento",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    stock_actual = table.Column<int>(type: "int", nullable: false),
                    stock_minimo = table.Column<int>(type: "int", nullable: false),
                    ubicacion = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventario_mantenimiento", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventario_mantenimiento");

            migrationBuilder.DropColumn(
                name: "anydesk_id",
                table: "solicitudes");

            migrationBuilder.CreateTable(
                name: "mensajes_privados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    adjunto_id = table.Column<int>(type: "int", nullable: true),
                    destinatario_id = table.Column<int>(type: "int", nullable: false),
                    remitente_id = table.Column<int>(type: "int", nullable: false),
                    solicitud_id = table.Column<int>(type: "int", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    leido_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SolicitudId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensajes_privados", x => x.id);
                    table.ForeignKey(
                        name: "FK_mensajes_privados_adjuntos_adjunto_id",
                        column: x => x.adjunto_id,
                        principalTable: "adjuntos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_mensajes_privados_solicitudes_SolicitudId1",
                        column: x => x.SolicitudId1,
                        principalTable: "solicitudes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_mensajes_privados_solicitudes_solicitud_id",
                        column: x => x.solicitud_id,
                        principalTable: "solicitudes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_mensajes_privados_usuarios_destinatario_id",
                        column: x => x.destinatario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_mensajes_privados_usuarios_remitente_id",
                        column: x => x.remitente_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_privados_adjunto_id",
                table: "mensajes_privados",
                column: "adjunto_id");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_privados_destinatario_id",
                table: "mensajes_privados",
                column: "destinatario_id");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_privados_remitente_id",
                table: "mensajes_privados",
                column: "remitente_id");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_privados_solicitud_id",
                table: "mensajes_privados",
                column: "solicitud_id");

            migrationBuilder.CreateIndex(
                name: "IX_mensajes_privados_SolicitudId1",
                table: "mensajes_privados",
                column: "SolicitudId1");
        }
    }
}
