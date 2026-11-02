# SolarGrid — pruebas para realizar antes de la exposición

**No están marcadas como aprobadas**: deben verificarse en Visual Studio. Anota el resultado real y toma una captura de pantalla.

| No. | Prueba manual | Resultado esperado | Estado |
|---|---|---|---|
| 1 | Entrar con `admin@solargrid.local` | Abre el panel principal | Sin ejecutar |
| 2 | Entrar con una contraseña errónea | Mensaje de acceso inválido | Sin ejecutar |
| 3 | Registrar una comunidad nueva | Aparece en la lista | Sin ejecutar |
| 4 | Registrar un sistema con comunidad | Aparece el sistema relacionado | Sin ejecutar |
| 5 | Registrar un equipo con código `PAN-003` | Aparece en el inventario | Sin ejecutar |
| 6 | Repetir exactamente `PAN-003` | Rechaza el código repetido | Sin ejecutar |
| 7 | Registrar lectura con 30 kWh y 20 kWh | Se agrega a la tabla | Sin ejecutar |
| 8 | Intentar energía = -5 | Muestra advertencia | Sin ejecutar |
| 9 | Registrar incidencia | Estado inicial Abierta | Sin ejecutar |
| 10 | Cambiar incidencia a Resuelta sin solución | Solicita una solución | Sin ejecutar |
| 11 | Consultar reporte por fechas | Muestra los totales y registros | Sin ejecutar |
| 12 | Invertir las fechas del reporte | Mensaje de fechas incorrectas | Sin ejecutar |
| 13 | Ingresar como usuario de comunidad | Puede reportar incidentes; no registrar equipos | Sin ejecutar |
| 14 | Mostrar SQL Server en Visual Studio | Aparecen las ocho tablas | Sin ejecutar |

## Evidencias sugeridas

1. Captura del panel principal.
2. Captura del formulario de equipos y la lista.
3. Captura de la validación de lectura negativa.
4. Captura de incidencia y seguimiento.
5. Captura del reporte por fechas.
6. Captura de las tablas en SQL Server.

Al terminar cada prueba, cambia "Sin ejecutar" por "Aprobada" o "Fallida". En caso de falla, registra el mensaje y cómo se corrigió. **No inventes capturas ni resultados.**
