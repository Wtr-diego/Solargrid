-- Comprobacion rapida de la base de SolarGrid en SSMS.
USE [solagrid];
GO
SELECT name AS Tabla
FROM sys.tables
WHERE schema_id = SCHEMA_ID('dbo')
ORDER BY name;
GO
SELECT 'Comunidades' AS Seccion, COUNT(*) AS Registros FROM dbo.Comunidades
UNION ALL SELECT 'Usuarios', COUNT(*) FROM dbo.Usuarios
UNION ALL SELECT 'SistemasSolares', COUNT(*) FROM dbo.SistemasSolares
UNION ALL SELECT 'Equipos', COUNT(*) FROM dbo.Equipos
UNION ALL SELECT 'UbicacionesPanel', COUNT(*) FROM dbo.UbicacionesPanel
UNION ALL SELECT 'LecturasEnergia', COUNT(*) FROM dbo.LecturasEnergia
UNION ALL SELECT 'Incidencias', COUNT(*) FROM dbo.Incidencias
UNION ALL SELECT 'Reportes', COUNT(*) FROM dbo.Reportes
UNION ALL SELECT 'Historial', COUNT(*) FROM dbo.Historial;
GO
-- Ubicacion guardada de cada panel para el mapa de Comunidades.
SELECT e.Codigo, e.Tipo, s.Nombre AS SistemaSolar, c.Nombre AS Comunidad,
       u.Latitud, u.Longitud, u.FechaActualizacion
FROM dbo.Equipos AS e
JOIN dbo.SistemasSolares AS s ON e.IdSistema = s.IdSistema
JOIN dbo.Comunidades AS c ON s.IdComunidad = c.IdComunidad
LEFT JOIN dbo.UbicacionesPanel AS u ON u.IdEquipo = e.IdEquipo
WHERE e.Tipo = N'Panel'
ORDER BY c.Nombre, e.Codigo;
GO
