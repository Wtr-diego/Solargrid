# Pruebas de la simulación solar

Ejecución: **8 de noviembre de 2026**, hora de El Salvador. La evidencia JSON conserva también la fecha UTC (9 de noviembre, 02:12 UTC). Aplicación ASP.NET Core .NET 8, SQL Server Express y Microsoft Edge controlado con Playwright.

Se utilizó exclusivamente la base **SolarGrid_Pruebas_Simulacion_20261108** en `http://127.0.0.1:5187`. Los registros de prueba no se insertaron en la base habitual del proyecto.

## Resultados

- Compilación: correcta, sin errores ni advertencias.
- Modelo matemático: **12 de 12 pruebas aprobadas**, mediante el ejecutable de `pruebas/`.
- Interfaz e integración: **26 de 26 pruebas aprobadas**, con cero errores JavaScript y cero errores de consola durante el recorrido comprobado.
- Comprobación adicional I27: Inicio pasó de 1 a 3 horas calculadas y de 0.09 a 0.24 kWh consumidos sin recargar la página. Actualización automática aprobada.

| Casos | Objetivo y entrada | Resultado esperado y obtenido |
|---|---|---|
| I01–I04 | Acceder como anónimo, Comunidad, Técnico y Administrador | Anónimo redirigido al ingreso; Comunidad sin acceso; Técnico y Administrador autorizados. Título y favicon de SolarGrid presentes. |
| I05 | Iniciar sin token antifalsificación | HTTP 400; no cambia el estado. |
| I06–I11 | Ubicación y clima desconocidos, potencia 0 o 455 W, 25 paneles, consumo 0 | HTTP 400 con mensaje; la sesión anterior permanece intacta. |
| I12 | Configurar Chalatenango, soleado, un panel de 450 W, consumo 1× | Escenario nuevo con coordenadas y parámetros seleccionados; cero lecturas iniciales. |
| I13 | Guardar con cero horas calculadas | HTTP 409; se solicita completar el día. |
| I14 | Iniciar con el identificador del escenario anterior | HTTP 409; se informa que otra pestaña cambió la sesión. |
| I15–I16 | Iniciar, pausar, continuar y avanzar una hora | El reloj avanza, se mantiene detenido durante la pausa y el avance manual agrega exactamente una lectura. |
| I17 | Completar el día | 24 horas distintas; no hay generación nocturna; se conservan los balances y coinciden los acumulados. |
| I18–I19 | Guardar, repetir dos veces y abrir el reporte del sistema y fecha | Un mismo sistema y exactamente 24 filas; generación y consumo del reporte coinciden con el simulador. |
| I20 | Consultar los datos del mapa después de guardar | Un panel virtual con código `SIM-…`, sistema `[SIM]` y coordenadas de Chalatenango. |
| I21 | Exportar CSV | 24 registros, 20 columnas, origen `SIMULADO` y valores del escenario. |
| I22 | Abrir el panel principal | Los indicadores coinciden con el día simulado. |
| I23–I25 | Escritorio 1440 × 1080, tema oscuro y móvil 390 × 844 | Métricas, gráfica y tabla utilizables; no desborda horizontalmente la página. La tabla móvil tiene desplazamiento propio. |
| I26 | Revisar errores del navegador durante el recorrido | Cero errores JavaScript y de consola. |

## Datos obtenidos en la demostración

Escenario: Chalatenango, un panel de 450 W, clima soleado, consumo 1×, día simulado **07/11/2026**.

| Indicador | Resultado |
|---|---:|
| Generación solar | 2.50 kWh |
| Consumo total | 5.15 kWh |
| Solar consumida directamente | 1.70 kWh |
| Energía requerida de la red | 3.45 kWh |
| Excedente solar | 0.80 kWh |
| Cobertura solar del consumo | 33.0 % |
| Lecturas guardadas | 24 |

Comprobación: `2.50 = 1.70 + 0.80` y `5.15 = 1.70 + 3.45`. Estos valores provienen del modelo didáctico, no de mediciones en una instalación física.

## Evidencias

- [Resultados de las 26 pruebas](evidencias/resultados-integracion.json)
- [Actualización de Inicio durante la ejecución](evidencias/resultados-inicio-en-vivo.json)
- [Estado completo y variables horarias](evidencias/estado-simulacion.json)
- [CSV exportado](evidencias/SolarGrid-simulacion.csv)
- [Simulador en escritorio](evidencias/simulacion-escritorio.png)
- [Resumen en el panel principal](evidencias/resumen-principal.png)
- [Reporte de las lecturas guardadas](evidencias/reporte-guardado.png)
- [Vista móvil](evidencias/simulacion-movil.png)
- [Tema oscuro](evidencias/simulacion-oscuro.png)
- [Ubicación del panel guardado](evidencias/paneles-mapa.json)

## Alcance y pruebas pendientes

No se identificaron errores funcionales en el recorrido ejecutado. Las pruebas validan coherencia matemática e integración con SolarGrid; no certifican precisión frente a un panel físico. El mapa se verificó mediante sus datos serializados, sin depender de la descarga de mosaicos de terceros.

Quedan fuera de esta ejecución las pruebas de carga con numerosos usuarios, varias instancias simultáneas del servidor, fallos de red durante una transacción y comparación con sensores meteorológicos o eléctricos reales. La sesión del simulador es compartida por una sola instancia y su avance no guardado se pierde al reiniciar el servidor, según el diseño documentado.

Para repetir las pruebas, ver [instrucciones del ejecutable y navegador](pruebas/README.md). El guion de navegador requiere confirmar expresamente una base de pruebas aislada, porque crea un escenario persistido.
