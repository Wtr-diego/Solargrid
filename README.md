# SolarGrid

Aplicación web académica para gestionar sistemas de energía solar comunitarios. Desarrollada en C# con ASP.NET Core MVC (.NET 8), Entity Framework Core 8 y SQL Server.

## Funcionalidades

- Inicio y cierre de sesión con roles Administrador, Técnico y Comunidad.
- Registro, consulta y edición de comunidades y equipos.
- Registro y consulta de sistemas solares asociados con comunidades.
- Registro manual de generación y consumo de energía.
- Reporte y seguimiento de incidencias.
- Reportes por fecha y sistema, con impresión desde el navegador.
- Ubicación de paneles en un mapa con Leaflet y OpenStreetMap.

## Ejecutar en Windows

Requisitos: SDK de .NET 8 y SQL Server Express o LocalDB. Visual Studio con la carga de trabajo de ASP.NET es opcional. El mapa requiere conexión a Internet.

```powershell
git clone https://github.com/Wtr-diego/Solargrid.git
cd Solargrid
Copy-Item appsettings.Development.example.json appsettings.Development.json
dotnet restore
dotnet run --launch-profile http
```

Abrir http://localhost:5088. La configuración de ejemplo usa LocalDB y la base `SolarGrid_Desarrollo`. Para SQL Server Express, editar `ConnectionStrings:SolarGrid` en la copia local de `appsettings.Development.json` con el nombre de la instancia correspondiente. Ese archivo está excluido de Git.

En modo Development, el proyecto crea una base nueva y datos ficticios si no existen usuarios. Para probar desde cero, usar una base nueva; no borrar una base que contenga información necesaria. La actualización general de esquemas mediante migraciones todavía está pendiente.

### Cuentas de demostración local

| Rol | Correo |
|---|---|
| Administrador | `admin@solargrid.local` |
| Técnico | `tecnico@solargrid.local` |
| Comunidad | `vecino@solargrid.local` |

La contraseña de estas cuentas ficticias es `DemoSolar2026!`, definida en `Data/DatosIniciales.cs`. Son datos públicos de demostración: no usar estas cuentas en un despliegue real. Publicar el código en GitHub no despliega la aplicación.

## Organización

| Carpeta | Contenido |
|---|---|
| `Controllers` | Acciones, autorización y validaciones de los módulos |
| `Models` | Entidades de la base de datos |
| `ViewModels` | Modelos y validaciones de formularios |
| `Data` | Contexto EF Core y datos iniciales |
| `Views` | Pantallas Razor |
| `wwwroot` | Estilos y JavaScript |
| `SQL` | Creación y verificación del esquema |
| `DOCUMENTACION/Entrega` | Informe y evidencias de la evaluación |

La base contempla nueve tablas: Comunidades, Usuarios, SistemasSolares, Equipos, UbicacionesPanel, LecturasEnergia, Incidencias, Reportes e Historial. Las tablas Reportes e Historial aún no reciben registros desde los flujos actuales.

## Estado y pruebas

Evaluación del **27 de octubre de 2026**: compilación correcta sin errores ni advertencias; **28 casos ejecutados, 26 aprobados y 2 fallidos**. Es un registro histórico, no una nueva ejecución al publicar el repositorio.

Hallazgos pendientes del formulario de equipos:

- Un código vacío puede provocar HTTP 500 por llamar `Trim()` antes de validar el modelo.
- Marca y Modelo se exigen implícitamente y muestran mensajes en inglés cuando están vacíos; debe definirse su obligatoriedad y ajustar las validaciones.

También quedan pendientes la administración de usuarios, el historial automático, la persistencia de reportes y pruebas adicionales de rendimiento, compatibilidad y recuperación.

- [Informe de avance técnico y pruebas](DOCUMENTACION/Entrega/Desarrollo%20Web%20-%20SolarGrid%20-%20Avance%20y%20pruebas.docx)
- [Resultados funcionales](DOCUMENTACION/Entrega/Evidencias/resultados.json)
- [Restricciones SQL comprobadas](DOCUMENTACION/Entrega/Evidencias/restricciones.txt)
- [Uso del mapa](COMO_USAR_EL_MAPA.md)
- [Guía de SQL Server](GUIA_SQL_SERVER.md)

Las capturas originales están en `DOCUMENTACION/Entrega/Evidencias`. Los archivos HTML de respuestas, scripts locales auxiliares, archivos temporales, compilaciones y configuración personal no se publican. Algunas guías iniciales describen estados anteriores del proyecto; para el estado comprobado consultar este README y el informe de entrega.
