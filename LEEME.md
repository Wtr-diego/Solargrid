# NUEVA FUNCIÓN: MAPA DE PANELES EN COMUNIDADES

Esta es la versión de SolarGrid **con mapa interactivo**. Entra a **Comunidades**, selecciona un panel que ya hayas registrado, haz clic en su posición y pulsa **Guardar ubicación**. Los paneles con coordenadas guardadas aparecen con un marcador solar; los antiguos sin coordenadas siguen en la lista para que los puedas ubicar. También puedes filtrar por comunidad, mover un marcador y quitar solo sus coordenadas. **NO necesitas borrar la base de datos anterior.**

El mapa usa Leaflet y OpenStreetMap, por lo que **se necesita Internet para cargar el mapa**. La información queda guardada en SQL Server.

Lee **`COMO_USAR_EL_MAPA.md`** para instrucciones y pruebas. Antes de actualizar tu copia personal, guarda una copia de seguridad de los archivos que hayas modificado.

---

# SOLARGRID — AVANCE GRANDE, EXPLICACIÓN SENCILLA

Este proyecto utiliza C#, ASP.NET Core MVC (.NET 8), SQL Server LocalDB y Entity Framework Core. Incluye **7 apartados visibles**, pero está organizado para que puedas presentarlo de forma fácil.

## 1. Lo que puedes mostrar

1. **Inicio de sesión** — 3 roles: administrador, técnico y comunidad.
2. **Comunidades** — listar, registrar y editar comunidades.
3. **Sistemas solares** — registrar sistemas y asociarlos a una comunidad.
4. **Equipos** — registrar y editar paneles, baterías e inversores, con código único.
5. **Lecturas de energía** — registrar producción y consumo en kWh.
6. **Incidencias** — reportar problemas y actualizar su estado. Un usuario de comunidad puede reportar problemas de su comunidad.
7. **Reportes** — consultar generación y consumo por fechas o por sistema, e imprimir la página.

Además, el **panel principal** enseña totales de comunidades, sistemas, equipos, lecturas y problemas activos, además de las últimas lecturas.

La base de datos contempla **8 tablas**: Usuarios, Comunidades, SistemasSolares, Equipos, LecturasEnergia, Incidencias, Reportes e Historial. Las últimas dos tablas están preparadas para futuras mejoras; la pantalla de reportes actual consulta las lecturas sin guardar un historial de reportes.

## 2. Qué instalar (Windows)

- **Visual Studio 2022** o superior, con la carga de trabajo **Desarrollo de ASP.NET y web**.
- **SDK de .NET 8**.
- **SQL Server Express LocalDB** (se puede instalar desde Visual Studio Installer).
- Internet la primera vez, para descargar paquetes de NuGet.

## 3. Abrir el proyecto (primera vez)

1. Extrae el archivo ZIP en el escritorio. **No lo abras directamente desde el ZIP.**
2. Abre Visual Studio y elige **Abrir un proyecto o solución**.
3. Selecciona **SolarGrid.csproj**.
4. Espera a que terminen de instalarse los paquetes.
5. Selecciona **https** en la parte superior de Visual Studio.
6. Presiona el botón verde **▶** o la tecla **F5**.
7. En la primera ejecución local, la base de datos se crea con ejemplos. La web normalmente abre `https://localhost:7088`.

Si aparece un error que menciona LocalDB o SQL Server, comprueba la instalación de **SQL Server Express LocalDB**. La cadena está en `appsettings.Development.json`.

## 4. Cuentas para entrar

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@solargrid.local` | `DemoSolar2026!` |
| Técnico | `tecnico@solargrid.local` | `DemoSolar2026!` |
| Comunidad | `vecino@solargrid.local` | `DemoSolar2026!` |

Son cuentas **solo para la demostración local**. No publicar el proyecto con estas credenciales.

## 5. Presentación rápida de 6 minutos

1. **Minuto 1:** abre SolarGrid, inicia sesión como administrador y muestra el panel (cinco indicadores).
2. **Minuto 2:** registra una comunidad y después un sistema solar relacionado.
3. **Minuto 3:** registra un equipo con código `PAN-003`, y muestra cómo rechaza registrar el mismo código otra vez.
4. **Minuto 4:** registra una lectura de energía (generación 30, consumo 20) y prueba con generación `-5` para mostrar la validación.
5. **Minuto 5:** crea una incidencia y cambia su estado a **En proceso**.
6. **Minuto 6:** abre **Reportes**, filtra por fechas y muestra las tablas de SQL Server.

**Importante:** practica este recorrido en tu computadora antes de presentarlo. Todo lo mostrado en este paquete es código preparado y documentación, no pruebas ya aprobadas.

## 6. Datos de ejemplo

Al crear por primera vez la base de datos de desarrollo, se cargan **2 comunidades, 2 sistemas solares, 5 equipos, 20 lecturas y 3 incidencias**, además de 3 cuentas. Los nombres de marcas y cantidades de energía son **ficticios**, solo para demostrar las pantallas.

Si ya habías iniciado una versión anterior y tienes una base de datos con ese nombre, **no se vuelven a cargar los ejemplos**, para no borrar tus datos. Si deseas una base de prueba desde cero y NO tienes información que necesites conservar, elimina la base `SolarGrid_PrimerAvance` desde el Explorador de objetos SQL Server de Visual Studio y vuelve a ejecutar el programa. Esto elimina todos los registros de esa base.

## 7. Validaciones que puedes demostrar

- No se admiten campos obligatorios vacíos.
- El correo y la contraseña deben coincidir con una cuenta registrada.
- No se aceptan códigos de equipos repetidos.
- Generación y consumo no pueden ser negativos.
- Las fechas de lectura no pueden estar en el futuro.
- No se puede vincular un equipo con una incidencia de otro sistema.
- Un problema resuelto o cerrado requiere texto de solución.
- No se puede generar un informe cuando la fecha inicial es posterior a la final.
- Las opciones para crear/editar se limitan según los permisos de usuario.

## 8. Qué falta para el proyecto final

- Crear y administrar usuarios desde la interfaz (RF-02).
- Búsquedas y filtros adicionales (RF-11).
- Guardar reportes generados como registros de tabla `Reportes` (RF-10 completo).
- Historial automático de modificaciones (RF-12).
- Pruebas automáticas, migraciones de base de datos y despliegue final.

## 9. Carpeta y arquitectura

```text
SolarGrid_PrimerAvance/
  Controllers/     Acciones: qué pasa al guardar y consultar
  Models/          Datos del sistema y sus tablas
  Views/           Pantallas que ve el usuario
  ViewModels/      Campos de los formularios y validaciones
  Data/            Base de datos y datos iniciales
  wwwroot/css/     Estilos y colores
  Program.cs       Configuración inicial
  SolarGrid.csproj Archivo que abres en Visual Studio
  DOCUMENTACION/   Informe listo para personalizar
```

**Estado de verificación:** en el entorno donde se preparó este paquete no está instalado el SDK `dotnet`; no se pudo compilar ni iniciar el programa aquí. Ejecuta F5 en Visual Studio y registra las pruebas realizadas de verdad antes de presentar. El proyecto contiene los archivos fuente, no ejecutables precompilados.
