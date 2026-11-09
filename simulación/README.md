# Simulación de energía solar en El Salvador

Este módulo forma parte de la aplicación ASP.NET Core de SolarGrid. Se abre desde **Simulación** en el menú y su resumen aparece en **Panel principal** para administradores y técnicos. No necesita instalar otro programa ni conectar un panel físico.

## Demostración para el equipo

1. Ejecutar SolarGrid con su conexión habitual a SQL Server (`dotnet run`) e iniciar sesión como Administrador o Técnico.
2. Abrir **Simulación**. Elegir San Salvador, Santa Ana, San Miguel o Chalatenango. Son puntos aproximados de referencia en El Salvador, no instalaciones reales.
3. Dejar un panel de **450 W**, consumo **1×** y clima **Variable**, o elegir otra configuración. Aplicar la configuración crea un nuevo día de demostración.
4. Pulsar **Iniciar**: cada 2 segundos se calcula una hora; el día completo tarda aproximadamente 48 segundos. **Pausar** detiene el reloj; **Avanzar una hora** permite explicar cada intervalo; **Completar día** obtiene las 24 horas inmediatamente.
5. Abrir el **Panel principal** en otra pestaña. Sus indicadores de simulación se actualizan automáticamente, incluso mientras la pestaña de Simulación está cerrada.
6. Mostrar la curva de generación y consumo. De noche la generación es cero; durante el día una parte del consumo se cubre con el panel. Explicar autoconsumo, importación y excedente con los indicadores.
7. Descargar el **CSV** para conservar todas las variables calculadas. Puede descargarse también un avance parcial.
8. Con el día completo, pulsar **Guardar lecturas**. Se registran 24 lecturas horarias en SQL Server, un sistema cuyo nombre comienza con `[SIM]`, sus paneles virtuales y sus ubicaciones. Repetir Guardar sobre la misma sesión no duplica registros.
9. Abrir **Lecturas**, **Reportes** (seleccionar el sistema `[SIM]` y la fecha del día simulado), **Sistemas**, **Equipos** o el mapa de **Comunidades**. La generación y el consumo de los reportes coinciden con los totales del simulador.
10. Crear otro escenario con clima **Nublado** y comparar. Los valores son deterministas: repetir los mismos parámetros produce los mismos resultados; cambiar solamente el clima mantiene la demanda y cambia la generación.

Las lecturas guardadas participan en los totales y alertas existentes. Se distinguen por el sistema `[SIM]` y la comunidad `[SIM] Demostración El Salvador`. Son registros de demostración, no producción medida.

## Qué significa cada dato

| Dato | Unidad | Significado |
|---|---|---|
| Potencia instalada | W | Potencia nominal por panel × cantidad de paneles |
| Irradiancia | W/m² | Radiación solar sintética en el intervalo |
| Temperatura ambiente / panel | °C | Temperaturas calculadas con supuestos didácticos |
| Potencia solar | W | Potencia estimada en el punto medio de la hora |
| Potencia de consumo | W | Demanda estimada del hogar en ese intervalo |
| Generación | kWh | Energía que producen los paneles |
| Consumo | kWh | Demanda total del hogar, incluyendo energía solar y red |
| Autoconsumo solar | kWh | Parte de la generación utilizada directamente por el hogar |
| Importación de red | kWh | Demanda que el panel no alcanza a cubrir |
| Excedente | kWh | Energía solar sobrante; no representa un pago ni una exportación física medida |
| Cobertura solar | % | Autoconsumo solar acumulado / consumo acumulado × 100 |

Balances comprobables en cada fila y en los totales:

```text
autoconsumo = mínimo(generación, consumo)
consumo = autoconsumo + importación de red
generación = autoconsumo + excedente
```

## Supuestos del modelo

- Un intervalo representa una hora y se calcula en su punto medio. La marca de tiempo identifica el inicio del intervalo, de 00:00 a 23:00.
- Se simula el día anterior en horario UTC−6. Así todas las lecturas pertenecen a una misma fecha pasada y se pueden filtrar en los reportes.
- Día idealizado con luz de 06:00 a 18:00: `altura = máximo(0, sen(π × (hora + 0.5 − 6) / 12))`.
- Irradiancia: `1000 × altura × factor del clima`. Soleado usa 0.95; Variable y Nublado usan curvas sintéticas reproducibles. No se consulta un servicio meteorológico.
- Cada ubicación tiene una temperatura base supuesta, no una media climatológica verificada. La temperatura ambiente varía durante el día; la del panel aumenta con la irradiancia.
- Potencia solar: `potencia instalada × irradiancia / 1000 × factor de temperatura × 0.86`, limitada entre cero y la potencia instalada. El factor de temperatura es `máximo(0, 1 − 0.004 × (temperatura del panel − 25))`. Las constantes representan un ejemplo educativo, no una ficha técnica certificada.
- Consumo base de ejemplo: 80 W de madrugada, 350 W por la mañana, 160 W durante el día, 420 W al anochecer y 120 W al final de la noche; incluye una variación reproducible del 8 % y el factor de consumo seleccionado.
- `energía (kWh) = potencia (W) / 1000 × 1 hora`. Cada energía se redondea a dos decimales **antes** de acumular, como las columnas existentes de SQL Server. La potencia se muestra con un decimal. Por eso puede existir una pequeña diferencia al recalcular la energía desde la potencia mostrada.
- No se modelan baterías, orientación, inclinación, sombras reales, estaciones, degradación, tarifas ni condiciones de una instalación específica. No sirve para dimensionar una instalación real o estimar una factura.

## Integración y archivos

| Archivo | Responsabilidad |
|---|---|
| `simulación/ModelosSimulacion.cs` | Configuración, límites, ubicaciones, lecturas y resumen |
| `simulación/ModeloSolar.cs` | Cálculo reproducible por hora |
| `simulación/SimuladorSolar.cs` | Reloj en segundo plano, controles, exclusión mutua y guardado transaccional |
| `Controllers/SimulacionController.cs` | Rutas protegidas, validación, respuestas JSON y descarga CSV |
| `Views/Simulacion/Index.cshtml` | Pantalla de presentación y configuración |
| `Views/Shared/_ResumenSimulacion.cshtml` | Resumen reutilizado en la página principal |
| `wwwroot/js/simulacion.js` | Controles, actualización periódica, gráfica y tabla |
| `wwwroot/css/simulacion.css` | Diseño adaptable y compatible con tema oscuro |
| `Program.cs` | Registro de una única instancia del simulador como servicio en segundo plano |
| `simulación/pruebas/` | Pruebas ejecutables del modelo sin paquetes de prueba adicionales |

GET: `/Simulacion`, `/Simulacion/Estado`, `/Simulacion/Exportar`.
POST con token antifalsificación: `/Simulacion/Configurar`, `/Iniciar`, `/Pausar`, `/Avanzar`, `/Completar`, `/Guardar` (todas bajo `/Simulacion`). Los controles envían el identificador del escenario; una pestaña desactualizada recibe HTTP 409 si intenta modificar otra sesión.

La simulación es **compartida por las pestañas y usuarios técnicos de una misma instancia del sitio**. Se conserva en memoria mientras el servidor está encendido; al reiniciarlo se pierde el avance no guardado y se prepara una nueva sesión. Las lecturas ya guardadas permanecen en SQL Server. Los detalles meteorológicos y los balances horarios completos se conservan mediante CSV; las tablas existentes almacenan generación y consumo, y el historial registra la configuración utilizada. No se cambia el esquema de la base de datos.

El guardado crea la comunidad de simulación si hace falta y realiza en una transacción la inserción del sistema, los equipos, sus ubicaciones, las 24 lecturas y una entrada de historial. El usuario que pulsa Guardar queda como autor. Una nueva configuración no borra registros guardados anteriormente.

## Verificación

```powershell
dotnet build --no-restore
dotnet run --project "simulación/pruebas/SolarGrid.Simulacion.Pruebas.csproj"
```

La carpeta `pruebas` está excluida de la compilación del sitio para que su programa de consola no se publique como parte de la aplicación web. Ver también `pruebas/README.md` y el registro de pruebas de integración `PRUEBAS.md`.
