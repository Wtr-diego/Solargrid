# SolarGrid: usar la base de datos desde SQL Server Management Studio (SSMS)

**Importante:** SSMS es el programa para consultar y administrar bases de datos. La informacion realmente se almacena en una **instancia de SQL Server**, por ejemplo `(.\SQLEXPRESS)` o `(localdb)\MSSQLLocalDB`.

La copia de SolarGrid que viene en este ZIP tiene su conexion `appsettings.Development.json` preparada para **`.\SQLEXPRESS`**. Si tu instancia de SQL Server tiene otro nombre, cambialo como se indica abajo.

## OPCION A: solo quiero ver y administrar la base que ya uso (recomendada si tengo datos)

No necesitas crear otra base ni mover datos:
1. Abre SSMS y en **Nombre del servidor** escribe `(localdb)\MSSQLLocalDB`.
2. Autenticacion: **Windows**. Conecta.
3. Expande **Bases de datos** -> **solagrid** -> **Tablas**.
4. En `dbo.Equipos` o `dbo.UbicacionesPanel` haz clic derecho -> **Seleccionar las 1000 filas principales**.
5. **En este caso usa la version anterior del proyecto**, con la conexion LocalDB; no necesitas el cambio a SQLEXPRESS que trae este ZIP.

## OPCION B: crear una base nueva en SQL Server Express para verla desde SSMS

1. Asegurate de que tienes instalado el **motor de SQL Server Express o Developer** (SSMS solo no instala un servidor).
2. Abre SSMS. Conectate a la misma instancia que usara SolarGrid. Un nombre habitual para Express es `.\SQLEXPRESS`; otras instalaciones pueden tener otro nombre.
3. Autenticacion: **Windows**.
4. Abre `SQL/01_Crear_Base_SolarGrid.sql` con **Archivo -> Abrir -> Archivo**. Pulsa **Ejecutar / F5**.
5. Se crea `solagrid` con **9 tablas**, claves, relaciones y restricciones. **No elimina registros que ya existan**.
6. En Visual Studio abre **SolarGrid.csproj** desde esta carpeta. Comprueba `appsettings.Development.json`:

   ```json
   "ConnectionStrings": {
     "SolarGrid": "Server=.\\SQLEXPRESS;Database=solagrid;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
   ```

   Esa muestra corresponde a JSON: dentro del archivo debe verse exactamente `Server=.\\SQLEXPRESS;...` (dos barras seguidas para representar una sola en el nombre de la instancia).
7. Si tu servidor se llama `MI-PC\SQLEXPRESS`, sustituye `Server=.\\SQLEXPRESS` por `Server=MI-PC\\SQLEXPRESS`. Si es una instancia predeterminada, por ejemplo `localhost`, usa `Server=localhost`.
8. Presiona F5 en Visual Studio. En una **base vacia**, SolarGrid agrega automaticamente datos de demostracion, incluidos usuarios con contrasenas protegidas. No agregues una contrasena normal como texto en SQL.
9. Regresa a SSMS, clic derecho en **Bases de datos -> Actualizar**. Abre `SQL/02_Verificar_Base.sql` y pulsa F5 para comprobar tablas, registros y puntos del mapa.

> Si la aplicacion no se conecta, comprueba el nombre del servidor con el que entras a SSMS. Copialo a la conexion de SolarGrid. Tu usuario de Windows debe tener permiso para crear, leer y modificar los datos de esa base de demostracion.

## OPCION C: ya tengo paneles y ubicaciones en LocalDB y quiero MOVERLOS a SQLEXPRESS

**NO ejecutes el script de creacion sobre una base donde ya restauraste datos. NO elimines ni sobrescribas la base anterior.**

1. Cierra SolarGrid (deten la depuracion de Visual Studio).
2. Abre SSMS y conectate a `(localdb)\MSSQLLocalDB`.
3. En **Bases de datos**, clic derecho en `solagrid` -> **Tareas -> Copia de seguridad** (*Back Up*). Tipo **Completa**. Escoge una ubicacion de archivo `.bak` accesible a la cuenta del servicio de SQL Server que lo restaurara.
4. Conectate a tu servidor de destino (ejemplo `.\SQLEXPRESS`). En **Bases de datos**, clic derecho -> **Restaurar base de datos**.
5. Selecciona el archivo `.bak` como **Dispositivo** y restaura a `solagrid`. Si ya existe otra base con ese nombre en el destino, **no la sobrescribas**: revisa antes la ubicacion y nombre de destino.
6. En **Archivos / Files** revisa las rutas del MDF/LDF y ajústalas al servidor de destino si SSMS lo pide. Es posible que los archivos del respaldo apunten a rutas de LocalDB.
7. Cambia la cadena de conexion en `appsettings.Development.json` para que apunte a la instancia de destino y ejecuta SolarGrid.
8. Comprueba desde SSMS que los paneles y ubicaciones anteriores estan en las tablas y aparecen en el mapa.
9. Conserva la copia antigua en LocalDB y el archivo `.bak` hasta confirmar que todo funciona.

**Nota de compatibilidad:** una copia SQL Server de una version mas nueva no puede restaurarse directamente en una version mas antigua.

## Explicacion breve para la exposicion

"La informacion de SolarGrid se guarda en una base SQL Server llamada `solagrid`. Desde SQL Server Management Studio podemos ver las nueve tablas, sus relaciones y los registros de comunidades, equipos, lecturas y ubicaciones. La pagina web usa Entity Framework Core para conectarse a esa misma base, asi que cuando registramos un panel o marcamos una ubicacion en el mapa, los datos quedan guardados y podemos consultarlos desde SSMS."

**Verificacion:** estos archivos se crearon tomando como referencia los modelos reales de SolarGrid, pero no se han ejecutado contra un SQL Server en este entorno. Realiza la prueba en tu computadora antes de presentar.
