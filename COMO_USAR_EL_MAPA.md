# SolarGrid — mapa de paneles solares

Esta versión agrega un mapa interactivo a la página **Comunidades** del sistema SolarGrid.

## Qué hace

- Muestra en el mapa los **paneles solares** (`Equipos` con `Tipo = Panel`) que ya tienen ubicación.
- También muestra una **lista de todos los paneles registrados**, aunque todavía no tengan ubicación.
- Permite elegir un panel existente, hacer clic en el mapa y guardar sus coordenadas en **SQL Server**.
- Permite cambiar la ubicación guardada o quitar únicamente las coordenadas **sin eliminar el panel**.
- Filtra marcadores y lista por comunidad.
- Mantiene las posiciones después de cerrar y volver a iniciar la aplicación.

**Nota importante:** los paneles registrados antes de instalar este cambio no tenían coordenadas. Aparecen en la lista como «Sin ubicación» hasta que los marques sobre el mapa. No es posible deducir su lugar exacto solo con el nombre de la comunidad.

## Cómo probarlo en Visual Studio (morado)

1. **Haz una copia de seguridad** de tu carpeta SolarGrid antes de sustituir archivos.
2. Descomprime este ZIP y abre `SolarGrid_PrimerAvance/SolarGrid.csproj` en **Visual Studio 2022** con .NET 8 y SQL Server Express LocalDB.
3. Pulsa **F5** y entra con una cuenta de prueba, por ejemplo:
   - Correo: `admin@solargrid.local`
   - Contraseña: `DemoSolar2026!`
4. Entra en la pestaña **Comunidades**. Debajo del título verás cuatro contadores, el mapa y la lista de paneles.
5. Selecciona un panel (por ejemplo, `PAN-001` o `PAN-002`).
6. Muévete sobre el mapa de El Salvador y haz clic en la posición del panel.
7. Verifica que salgan latitud y longitud y pulsa **Guardar ubicación**.
8. Aparecerá un marcador con un sol en el mapa. Actualiza la página para verificar que siga allí.
9. Repite para otro panel. Puedes filtrar el mapa por comunidad.
10. Haz clic en un marcador o en **Ver / mover** para cambiar la posición de un panel.

Si creaste paneles nuevos desde **Equipos → Nuevo equipo**, seleccionando el tipo **Panel**, aparecerán automáticamente en la lista de Comunidades.

## Qué cambia en la base de datos

Se agrega una tabla nueva llamada `UbicacionesPanel`:

| Campo | Significado |
|---|---|
| `IdEquipo` | Identifica el panel. Es clave primaria y foránea a `Equipos`. |
| `Latitud` | Coordenada entre -90 y 90. |
| `Longitud` | Coordenada entre -180 y 180. |
| `FechaActualizacion` | Última fecha en que se guardó su ubicación. |

La tabla se crea automáticamente la primera vez que arrancas la aplicación **en desarrollo**, tanto con una base nueva como con una base de la versión anterior. No se eliminan las tablas anteriores ni los equipos existentes. Se comprueba que el equipo sea un `Panel` antes de guardar.

## Archivos modificados o agregados

- `Models/Entidades.cs`: entidad `UbicacionPanel` vinculada a un equipo.
- `Data/SolarGridDbContext.cs`: relación uno a uno entre panel y ubicación.
- `Data/DatosIniciales.cs`: actualización de base de datos sin borrar registros previos.
- `Controllers/ComunidadesController.cs`: carga del mapa, guardado y cambio de ubicaciones.
- `ViewModels/MapaComunidadesVm.cs`: datos que se muestran en la pantalla.
- `Views/Comunidades/Index.cshtml`: mapa, filtros, paneles y formulario.
- `Views/Shared/_Layout.cshtml`: mensajes de error.
- `wwwroot/css/site.css`: estilo del mapa.
- `wwwroot/js/mapa-comunidades.js`: eventos de clic y marcadores.

## Internet y bibliotecas

- El mapa usa **Leaflet 1.9.4** para los controles y marcadores.
- Los mapas base vienen de **OpenStreetMap**. Leaflet y las imágenes de calles se descargan desde Internet cuando visitas el mapa. Es necesaria conexión para verlo.
- Las coordenadas registradas **sí permanecen en SQL Server** aunque cierres el navegador o se interrumpa Internet.
- El mapa de OSM permite seleccionar coordenadas geográficas; no controla equipos eléctricos físicamente.

## Pruebas que debes ejecutar antes de presentar

- [ ] Abre Comunidades y aparece el mapa.
- [ ] Se ven todos los paneles existentes en la lista.
- [ ] Un panel sin ubicación puede marcarse haciendo clic y guardarse.
- [ ] Al recargar la página, su marcador sigue apareciendo.
- [ ] Puedes mover su marcador guardando una nueva ubicación.
- [ ] El filtro de comunidad oculta los marcadores de otras comunidades.
- [ ] Quitar ubicación NO elimina el equipo.
- [ ] No se guarda si no has elegido panel y posición.
- [ ] Revisa que puedas entrar en Sistemas, Equipos y Lecturas como antes.

**Estado:** archivos fuente preparados. Esta ejecución no incluyó compilación en Visual Studio ni pruebas de base de datos porque el entorno no dispone del SDK `dotnet` ni de SQL Server. No marques las pruebas como aprobadas hasta comprobarlas en tu PC.

## Qué decir en la presentación

«Agregamos un mapa a Comunidades. Los paneles ya registrados aparecen en una lista y se pueden ubicar con un clic en el mapa. La ubicación queda guardada en la base de datos y, al volver a entrar, el marcador se mantiene. También podemos filtrar por comunidad y modificar una posición si fue registrada incorrectamente».
