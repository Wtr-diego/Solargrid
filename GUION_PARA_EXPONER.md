# Guion de exposición (sencillo, aproximadamente 5 a 6 minutos)

**Inicio:** “Nuestro proyecto se llama SolarGrid y ayuda a organizar la información de sistemas de energía solar en comunidades. Ya no tendríamos que llevar esos datos en hojas separadas.”

**Panel:** “Aquí tenemos el panel principal. Podemos ver las comunidades, los sistemas solares, los equipos, las lecturas de energía y los problemas que todavía están pendientes.”

**Comunidades y sistemas:** “Primero registramos las comunidades. Después, creamos los sistemas solares y los asociamos a la comunidad a la que pertenecen.”

**Equipos:** “Agregamos los paneles, baterías e inversores con un código que no se puede repetir. De esta forma sabemos qué equipos tiene cada sistema.”

**Energía:** “Podemos registrar cuánta energía se produce y cuánto se consume. El sistema valida que no guardemos cantidades negativas o fechas futuras.”

**Problemas:** “También permite reportar problemas de los sistemas. Un técnico puede cambiar el estado del problema cuando empieza a atenderlo o cuando ya lo resuelve.”

**Reportes:** “Aquí consultamos los datos de energía por un rango de fechas y podemos ver los totales de producción y consumo.”

**Base de datos:** “La información se guarda en SQL Server. Tenemos ocho tablas relacionadas para ordenar las comunidades, usuarios, sistemas, equipos, lecturas, incidencias, reportes e historial.”

**Cierre:** “Este avance cubre las funciones principales. Nos queda completar la gestión de usuarios, el historial automático y algunas mejoras a los reportes antes de entregar la versión final.”

**Nota:** Di “ya funciona” únicamente después de comprobarlo en tu Visual Studio. Si algo falla en la prueba, menciona que está en desarrollo.


## Demostración adicional — mapa de paneles (1-2 minutos)

1. Entra a **Comunidades**. Señala el nuevo mapa y el total de paneles.
2. Abajo, busca `PAN-001` en la lista; al principio dirá **Sin ubicación** si nunca lo marcaste.
3. Selecciónalo, haz clic en el mapa sobre su ubicación real o una posición ficticia solo para la demostración, y pulsa **Guardar ubicación**.
4. Recarga Comunidades: aparece el marcador con un sol.
5. Explica que un mismo mapa muestra todos los paneles y que se pueden filtrar por comunidad.
6. Señala que los datos quedan guardados en la tabla nueva `UbicacionesPanel` y que no fue necesario borrar equipos previos.

**Practica este recorrido antes de mostrarlo; las ubicaciones de ejemplo no deben presentarse como instalaciones reales.**
