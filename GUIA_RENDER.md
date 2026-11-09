# Publicar SolarGrid en Render

Se publica la aplicación ASP.NET Core con Docker y se reutiliza la base SQL Server de Somee. No se necesita trasladar las tablas ni crear una base PostgreSQL. Los cambios realizados desde cualquiera de los dos sitios afectan la misma base.

## Publicación desde GitHub

1. Subir a la rama `main` los cambios, incluyendo `Dockerfile`, `.dockerignore` y `render.yaml`.
2. Entrar en [Render](https://dashboard.render.com/) y elegir **New → Blueprint**.
3. Conectar GitHub y seleccionar `Wtr-diego/Solargrid`, rama `main`. Usar `render.yaml` de la raíz.
4. Render pedirá **ConnectionStrings__SolarGrid**. Pegar la cadena completa de conexión SQL de Somee. Usar el valor del archivo privado local `PUBLICACION/Render/CONEXION_SQL_PRIVADA.txt`, si ya se generó en esta computadora. No incluir el nombre de la variable en su valor.
5. Revisar que el servicio `solargrid` indique **Free**, y pulsar **Deploy Blueprint**. No añadir bases ni servicios de pago.
6. Esperar el estado **Live** y abrir la dirección HTTPS `*.onrender.com` que asigne Render. Iniciar sesión con la cuenta que ya funciona en Somee.

No subir archivos ZIP de Somee a Render: Render construye la aplicación desde el código de GitHub. La contraseña SQL es distinta de la contraseña para iniciar sesión en SolarGrid.

## Alternativa: New → Web Service

Si se crea el servicio manualmente, seleccionar el mismo repositorio y estos valores:

| Campo | Valor |
|---|---|
| Branch | `main` |
| Language / Runtime | `Docker` |
| Root Directory | Dejar vacío |
| Dockerfile Path | `./Dockerfile` |
| Instance Type | `Free` |
| Health Check Path | `/Cuenta/Ingresar` |

Agregar las siguientes variables en **Environment** antes de desplegar:

| Nombre | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `10000` |
| `PORT` | `10000` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |
| `ConnectionStrings__SolarGrid` | Cadena completa privada de SQL Server |

La variable de conexión usa **dos guiones bajos**. No colocar credenciales en GitHub, Dockerfile ni render.yaml. El archivo privado de PUBLICACION está excluido de Git y del contexto Docker.

## Verificar el despliegue

- Abrir el login por HTTPS: debe cargar con estilos y el botón Mostrar/Ocultar contraseña.
- Iniciar sesión con la cuenta existente: esta prueba confirma la conexión real a SQL Server. El chequeo automático de Render solo verifica que la pantalla de acceso responda; no consulta la base.
- Abrir **Espacio de trabajo** y comprobar que se desplieguen los enlaces.
- Como administrador, abrir **Usuarios**: deben aparecer las tablas Administradores, Técnicos y Usuarios de comunidad.
- Abrir Simulación, completar un día y comprobar los indicadores de Inicio. Guardar las lecturas únicamente cuando se quieran conservar en la base compartida.

Si el login muestra un error de conexión, revisar **Logs**, el valor privado y la disponibilidad de Somee. La conectividad de Render hacia Somee solo queda confirmada al probar el servicio desplegado. No reinstalar ni borrar las tablas para solucionar una contraseña incorrecta.

## Funcionamiento del plan gratuito

Render suspende los servicios gratuitos después de 15 minutos sin tráfico entrante; la primera visita posterior tarda aproximadamente un minuto. Los reinicios eliminan el estado en memoria de la simulación y pueden requerir iniciar sesión otra vez. Las lecturas ya guardadas siguen en la base externa de Somee, mientras esa base continúe disponible.

La aplicación no crea cuentas de demostración ni tablas en Production. Esta configuración presupone que la base de Somee ya está instalada. Una base nueva requiere instalar el esquema y una cuenta inicial antes de usarla.

El servicio recibe HTTP en el puerto 10000 detrás del HTTPS administrado por Render. `ASPNETCORE_FORWARDEDHEADERS_ENABLED` permite que ASP.NET Core reconozca el protocolo original que informa ese proxy. Habilitar esta opción solo en un entorno con un proxy de confianza; no exponer directamente ese contenedor a clientes no confiables.

Tras conectar el servicio, cada cambio que se suba a `main` iniciará un despliegue automático. Mantener el servicio en Free y revisar los límites de uso en el panel.

## Referencias

- [Docker en Render](https://render.com/docs/docker)
- [Configuración Blueprint](https://render.com/docs/blueprint-spec)
- [Límites gratuitos](https://render.com/docs/free)
- [ASP.NET Core detrás de un proxy](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-8.0)
