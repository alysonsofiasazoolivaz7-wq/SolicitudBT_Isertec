using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolicitudIT_Isertec.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permisos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    clave = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    grupo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permisos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    usuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "equipos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    marca = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    numero_serie = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    usuario_id = table.Column<int>(type: "int", nullable: true),
                    ubicacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipos", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "usuario_permisos",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    permiso_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_permisos", x => new { x.usuario_id, x.permiso_id });
                    table.ForeignKey(
                        name: "FK_usuario_permisos_permisos_permiso_id",
                        column: x => x.permiso_id,
                        principalTable: "permisos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuario_permisos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "solicitudes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    categoria_id = table.Column<int>(type: "int", nullable: false),
                    equipo_id = table.Column<int>(type: "int", nullable: true),
                    tecnico_id = table.Column<int>(type: "int", nullable: true),
                    tecnico_solicitado_id = table.Column<int>(type: "int", nullable: true),
                    titulo = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    prioridad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    puede_continuar_trabajando = table.Column<bool>(type: "bit", nullable: false),
                    modalidad_atencion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fecha_visita_solicitada = table.Column<DateOnly>(type: "date", nullable: true),
                    hora_visita_solicitada = table.Column<TimeSpan>(type: "time", nullable: true),
                    fecha_solicitada = table.Column<DateOnly>(type: "date", nullable: true),
                    hora_inicio_solicitada = table.Column<TimeSpan>(type: "time", nullable: true),
                    hora_fin_solicitada = table.Column<TimeSpan>(type: "time", nullable: true),
                    fecha_programada = table.Column<DateOnly>(type: "date", nullable: true),
                    hora_inicio_programada = table.Column<TimeSpan>(type: "time", nullable: true),
                    hora_fin_programada = table.Column<TimeSpan>(type: "time", nullable: true),
                    solucion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
                    resuelto_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cerrado_en = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitudes", x => x.id);
                    table.ForeignKey(
                        name: "FK_solicitudes_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_solicitudes_equipos_equipo_id",
                        column: x => x.equipo_id,
                        principalTable: "equipos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_solicitudes_usuarios_tecnico_id",
                        column: x => x.tecnico_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_solicitudes_usuarios_tecnico_solicitado_id",
                        column: x => x.tecnico_solicitado_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_solicitudes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "adjuntos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    solicitud_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    nombre_original = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    nombre_archivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ruta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    tipo_mime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tamano = table.Column<long>(type: "bigint", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adjuntos", x => x.id);
                    table.ForeignKey(
                        name: "FK_adjuntos_solicitudes_solicitud_id",
                        column: x => x.solicitud_id,
                        principalTable: "solicitudes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_adjuntos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comentarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    solicitud_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    comentario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comentarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_comentarios_solicitudes_solicitud_id",
                        column: x => x.solicitud_id,
                        principalTable: "solicitudes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comentarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historial_solicitudes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    solicitud_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    estado_anterior = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    estado_nuevo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_solicitudes", x => x.id);
                    table.ForeignKey(
                        name: "FK_historial_solicitudes_solicitudes_solicitud_id",
                        column: x => x.solicitud_id,
                        principalTable: "solicitudes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_historial_solicitudes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    solicitud_id = table.Column<int>(type: "int", nullable: true),
                    tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    enlace = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    leida_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_notificaciones_solicitudes_solicitud_id",
                        column: x => x.solicitud_id,
                        principalTable: "solicitudes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notificaciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mensajes_privados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    solicitud_id = table.Column<int>(type: "int", nullable: false),
                    remitente_id = table.Column<int>(type: "int", nullable: false),
                    destinatario_id = table.Column<int>(type: "int", nullable: false),
                    adjunto_id = table.Column<int>(type: "int", nullable: true),
                    mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    leido_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                name: "IX_adjuntos_solicitud_id",
                table: "adjuntos",
                column: "solicitud_id");

            migrationBuilder.CreateIndex(
                name: "IX_adjuntos_usuario_id",
                table: "adjuntos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_comentarios_solicitud_id",
                table: "comentarios",
                column: "solicitud_id");

            migrationBuilder.CreateIndex(
                name: "IX_comentarios_usuario_id",
                table: "comentarios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipos_usuario_id",
                table: "equipos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_solicitudes_solicitud_id",
                table: "historial_solicitudes",
                column: "solicitud_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_solicitudes_usuario_id",
                table: "historial_solicitudes",
                column: "usuario_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_solicitud_id",
                table: "notificaciones",
                column: "solicitud_id");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_usuario_id",
                table: "notificaciones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_categoria_id",
                table: "solicitudes",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_equipo_id",
                table: "solicitudes",
                column: "equipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_tecnico_id",
                table: "solicitudes",
                column: "tecnico_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_tecnico_solicitado_id",
                table: "solicitudes",
                column: "tecnico_solicitado_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_usuario_id",
                table: "solicitudes",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_permisos_permiso_id",
                table: "usuario_permisos",
                column: "permiso_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comentarios");

            migrationBuilder.DropTable(
                name: "historial_solicitudes");

            migrationBuilder.DropTable(
                name: "mensajes_privados");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropTable(
                name: "usuario_permisos");

            migrationBuilder.DropTable(
                name: "adjuntos");

            migrationBuilder.DropTable(
                name: "permisos");

            migrationBuilder.DropTable(
                name: "solicitudes");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "equipos");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
