# SolarGrid

Aplicación web académica para gestionar sistemas de energía solar comunitarios. Desarrollada en C# con ASP.NET Core MVC (.NET 8), Entity Framework Core 8 y SQL Server.

## Funcionalidades

- Inicio y cierre de sesión con roles Administrador, Técnico y Comunidad.
- Botón Mostrar/Ocultar contraseña al iniciar sesión y crear usuarios.
- Gestión de usuarios desde el menú **Usuarios** del administrador: alta, edición, asignación de rol y comunidad, desactivación y reactivación.
- Usuarios organizados en tres tablas por rol: administradores, técnicos y usuarios de comunidad.
- Menú lateral **Espacio de trabajo** con las opciones visibles según el rol.
- Historial de usuarios con la fecha, el administrador responsable, la cuenta afectada y los campos que cambiaron.
- Registro, consulta y edición de comunidades y equipos.
- Registro y consulta de sistemas solares asociados con comunidades.
- Registro manual de generación y consumo de energía.
- Simulación solar en El Salvador: escenarios de clima, generación y consumo por hora, resumen automático en Inicio, exportación CSV y guardado de lecturas identificadas con `[SIM]`.
- Reporte y seguimiento de incidencias.
- Reportes por fecha y sistema, con impresión desde el navegador.
- Ubicación de paneles en un mapa con Leaflet y OpenStreetMap.
- Tema claro y oscuro, logo en la pestaña del navegador y título `SolarGrid · sección actual`.

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

## Publicar en Render

El repositorio incluye `Dockerfile`, `.dockerignore` y `render.yaml` para crear un servicio **Free** desde **New → Blueprint** en Render. Se reutiliza la base SQL Server de Somee mediante la variable privada `ConnectionStrings__SolarGrid`; no se incluyen contraseñas en la imagen ni en GitHub.

Consultar la [guía de publicación en Render](GUIA_RENDER.md) para configurar el servicio y verificarlo. En el plan gratuito el servicio se suspende tras 15 minutos sin visitas: se pierde la simulación en memoria al reiniciarse, pero las lecturas guardadas permanecen en la base externa. La preparación del repositorio no implica que el sitio ya esté desplegado.

## Organización

| Carpeta | Contenido |
|---|---|
| `Controllers` | Acciones, autorización y validaciones de los módulos |
| `Models` | Entidades de la base de datos |
| `ViewModels` | Modelos y validaciones de formularios |
| `Data` | Contexto EF Core y datos iniciales |
| `Views` | Pantallas Razor |
| `wwwroot` | Estilos y JavaScript |
| `simulación` | Modelo solar, servicio de simulación, guía de presentación y pruebas |
| `SQL` | Creación y verificación del esquema |
| `DOCUMENTACION/Entrega` | Informe y evidencias de la evaluación |

La base contempla nueve tablas: Comunidades, Usuarios, SistemasSolares, Equipos, UbicacionesPanel, LecturasEnergia, Incidencias, Reportes e Historial. La tabla Historial guarda los cambios de cuentas y el registro de escenarios simulados guardados. La tabla Reportes aún no recibe registros desde los flujos actuales.

## Presentar la simulación

1. Iniciar sesión como **Administrador** o **Técnico** y abrir **Simulación**.
2. Elegir San Salvador, Santa Ana, San Miguel o Chalatenango; seleccionar clima soleado, variable o nublado, potencia y cantidad de paneles, y factor de consumo.
3. Pulsar **Aplicar escenario y reiniciar**, y después **Iniciar**. Cada dos segundos representan una hora: un día completo tarda aproximadamente 48 segundos.
4. Usar **Pausar**, **Continuar**, **Avanzar 1 hora** o **Completar el día** según el ritmo de la presentación.
5. Abrir **Inicio** en otra pestaña para observar cómo se actualizan los indicadores automáticamente.
6. Al terminar, pulsar **Guardar 24 lecturas**. El escenario aparecerá en Lecturas, Reportes, Sistemas, Equipos y el mapa con un nombre que comienza por `[SIM]`. **Ver reporte guardado** abre el sistema y la fecha correspondientes.
7. Usar **Descargar CSV** para conservar todas las variables horarias. Se puede descargar también un avance parcial.

El módulo muestra generación solar, consumo total, energía solar consumida directamente, energía requerida de la red, excedente y cobertura solar. Incluye una gráfica por hora y una tabla con irradiancia, temperaturas y potencias. La fecha simulada es el día anterior en horario de El Salvador (UTC−6).

Ejemplo comprobado: **Chalatenango, un panel de 450 W, soleado y consumo 1×** produce 2.50 kWh en el día simulado, con 5.15 kWh de consumo, 1.70 kWh de autoconsumo solar, 3.45 kWh requeridos de la red y 0.80 kWh de excedente. Son resultados del modelo didáctico, no mediciones reales.

![Resumen de la simulación en el panel principal](simulaci%C3%B3n/evidencias/resumen-principal.png)

El simulador genera datos sintéticos, sin sensores ni clima en tiempo real. La ejecución es compartida y permanece en memoria hasta detener el servidor; los registros guardados permanecen en SQL Server. La [guía de la simulación](simulaci%C3%B3n/README.md) explica el modelo, los controles, el guardado y cómo presentar los resultados al equipo.

## Estado y pruebas

La simulación se verificó el **8 de noviembre de 2026**: compilación sin errores ni advertencias y **39 comprobaciones aprobadas** (12 del modelo matemático, 26 de interfaz e integración y una adicional de actualización automática de Inicio). El guardado se probó en una base aislada, con 24 lecturas, totales iguales al reporte y sin duplicados al repetir la solicitud. Se comprobó la interfaz en escritorio, móvil y tema oscuro.

- [Plan, resultados y evidencias de la simulación](simulaci%C3%B3n/PRUEBAS.md)
- [Instrucciones de las pruebas del modelo y navegador](simulaci%C3%B3n/pruebas/README.md)

Para comprobar el modelo sin modificar la base de datos:

```powershell
dotnet build
dotnet run --project "simulación/pruebas/SolarGrid.Simulacion.Pruebas.csproj"
```

Evaluación del **27 de octubre de 2026**: compilación correcta sin errores ni advertencias; **28 casos ejecutados, 26 aprobados y 2 fallidos**. Es un registro histórico, no una nueva ejecución al publicar el repositorio.

Hallazgos pendientes del formulario de equipos:

- Un código vacío puede provocar HTTP 500 por llamar `Trim()` antes de validar el modelo.
- Marca y Modelo se exigen implícitamente y muestran mensajes en inglés cuando están vacíos; debe definirse su obligatoriedad y ajustar las validaciones.

También quedan pendientes la persistencia de reportes y pruebas adicionales de rendimiento, compatibilidad y recuperación.

- [Informe de avance técnico y pruebas](DOCUMENTACION/Entrega/Desarrollo%20Web%20-%20SolarGrid%20-%20Avance%20y%20pruebas.docx)
- [Resultados funcionales](DOCUMENTACION/Entrega/Evidencias/resultados.json)
- [Restricciones SQL comprobadas](DOCUMENTACION/Entrega/Evidencias/restricciones.txt)
- [Uso del mapa](COMO_USAR_EL_MAPA.md)
- [Guía de SQL Server](GUIA_SQL_SERVER.md)
- [Guía del proyecto para el equipo](DOCUMENTACION/Guia%20del%20proyecto%20SolarGrid%20para%20el%20equipo.docx) (describe el proyecto antes de añadir la simulación; complementar con su guía específica).

Las capturas originales están en `DOCUMENTACION/Entrega/Evidencias`. Los archivos HTML de respuestas, scripts locales auxiliares, archivos temporales, compilaciones y configuración personal no se publican. Algunas guías iniciales describen estados anteriores del proyecto; para el estado comprobado consultar este README y el informe de entrega.
