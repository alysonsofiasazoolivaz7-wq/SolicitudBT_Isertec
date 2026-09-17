# Migración PHP V3 → V4

## Equivalencias
- MySQL AUTO_INCREMENT → SQL Server IDENTITY.
- ENUM → enums C# almacenados como texto.
- TINYINT(1) → bit/bool.
- DATE → date / DateOnly.
- TIME → time / TimeSpan.
- TIMESTAMP/DATETIME → datetime2.
- `usuario` → propiedad C# `UsuarioLogin` con mapeo explícito.
- `comentario` → `ComentarioTexto` con mapeo explícito.
- `adjunto_id` se conserva en mensajes privados.

## Recomendación
Para datos existentes, exporta MySQL a CSV/SQL y realiza una migración controlada. No borres la base PHP hasta validar usuarios, solicitudes, adjuntos y relaciones.
