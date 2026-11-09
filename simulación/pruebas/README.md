# Pruebas del modelo solar

Este ejecutable comprueba el modelo matemático sin conectarse a SQL Server, iniciar el sitio ni modificar datos. Usa el proyecto principal mediante `ProjectReference`, por lo que verifica el mismo código que utiliza la página.

Desde la carpeta raíz de SolarGrid, ejecutar:

```powershell
dotnet run --project "simulación/pruebas/SolarGrid.Simulacion.Pruebas.csproj"
```

Requiere el SDK de .NET 8 o posterior y las dependencias habituales del proyecto principal. No agrega paquetes NuGet de pruebas. Cada comprobación imprime `APROBADA` o `FALLIDA`; al terminar devuelve código de salida `0` si todas pasan y `1` si existe alguna falla.

## Qué se verifica

1. Ausencia de generación e irradiancia nocturna y abastecimiento de la demanda mediante importación.
2. Valores energéticos no negativos, conservación de generación y consumo, y exclusión de importación y excedentes simultáneos en las cuatro ubicaciones y los tres climas.
3. Precisión de dos decimales compatible con `LecturasEnergia` y conversión de potencia a energía en intervalos de una hora.
4. Efecto del clima sobre la generación sin modificar la demanda.
5. Escalado de potencia y energía al aumentar el número de paneles.
6. Equivalencia entre configuraciones de la misma capacidad nominal.
7. Efecto del factor de consumo sin alterar la generación solar.
8. Reproducibilidad con las mismas entradas.
9. Fechas y horas de las 24 lecturas dentro del día seleccionado.
10. Configuraciones mínima y máxima admitidas.
11. Rechazo de horas fuera de rango.
12. Rechazo de ubicaciones y climas desconocidos.

Estas pruebas comprueban coherencia del modelo didáctico. No validan su exactitud frente a mediciones de un panel real. Las comprobaciones de interfaz, autenticación, guardado en SQL y reportes se realizan por separado en el sitio y en una base de datos de pruebas.

## Pruebas del sitio en un navegador

`integracion.cjs` automatiza Edge mediante Playwright. Comprueba acceso por roles, validaciones, controles, persistencia sin duplicados, reportes, CSV, actualización del inicio y presentación en escritorio, móvil y tema oscuro. Escribe resultados y capturas en `simulación/evidencias/`.

Antes de ejecutarlo, inicia el sitio en el puerto 5187 con una base **aislada** cuyo nombre comience por `SolarGrid_Pruebas_`. El guion crea escenarios y guarda registros; no debe ejecutarse contra la base del proyecto que se utiliza normalmente. La variable de confirmación es una declaración del operador: el navegador no puede comprobar qué base utiliza el servidor.

Requiere Node.js, Edge y el paquete `playwright`, instalado en el entorno de pruebas, por ejemplo con `npm install --no-save playwright`. Si ya tienes Playwright en otra carpeta, puedes establecer `PLAYWRIGHT_MODULE` con su ruta para reutilizarlo.

```powershell
$env:BASE_URL='http://127.0.0.1:5187'
$env:SOLARGRID_TEST_DB_CONFIRM='SolarGrid_Pruebas_Simulacion_20261108'
node 'simulación/pruebas/integracion.cjs'
```

Para comprobar únicamente la actualización automática de Inicio, además de sus controles de acceso iniciales:

```powershell
node 'simulación/pruebas/integracion.cjs' --home-live-only
```

Las pruebas del mapa revisan el conjunto de paneles y sus coordenadas servido por el proyecto. No dependen de descargar Leaflet ni los mosaicos de OpenStreetMap.
