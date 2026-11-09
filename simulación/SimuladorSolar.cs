using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;

namespace SolarGrid.Simulacion;

// Una sesión compartida por la instancia del sitio; los cálculos no dependen de que una pestaña esté abierta.
public sealed class SimuladorSolar(IServiceScopeFactory scopes) : BackgroundService
{
    private readonly SemaphoreSlim mutex = new(1, 1);
    private Guid id = Guid.NewGuid();
    private string estado = "Lista";
    private DateTime fecha = AyerEnElSalvador();
    private ConfiguracionSimulacion configuracion = new();
    private readonly List<LecturaSimulada> lecturas = new();
    private int? idSistemaGuardado;

    private static DateTime AyerEnElSalvador() => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-6).Date.AddDays(-1), DateTimeKind.Unspecified);
    private EstadoSimulacion Instantanea() => new()
    {
        Id = id, Estado = estado, Fecha = fecha, Configuracion = configuracion,
        IdSistemaGuardado = idSistemaGuardado, Lecturas = lecturas.ToArray()
    };

    private void ComprobarSesion(Guid? esperado)
    {
        if (esperado.HasValue && esperado != id)
            throw new InvalidOperationException("Otra pestaña cambió la simulación. Actualiza los datos antes de continuar.");
    }

    public async Task<EstadoSimulacion> ObtenerAsync(CancellationToken ct = default)
    {
        await mutex.WaitAsync(ct);
        try { return Instantanea(); }
        finally { mutex.Release(); }
    }

    public async Task<EstadoSimulacion> ConfigurarAsync(ConfiguracionSimulacion nueva, Guid? esperado, CancellationToken ct)
    {
        nueva.Validar();
        await mutex.WaitAsync(ct);
        try
        {
            ComprobarSesion(esperado);
            if (estado == "En ejecución") throw new InvalidOperationException("Pausa la simulación antes de crear otro escenario.");
            configuracion = nueva;
            id = Guid.NewGuid();
            fecha = AyerEnElSalvador();
            lecturas.Clear();
            estado = "Lista";
            idSistemaGuardado = null;
            return Instantanea();
        }
        finally { mutex.Release(); }
    }

    public async Task<EstadoSimulacion> EjecutarAsync(string accion, Guid? esperado, CancellationToken ct)
    {
        await mutex.WaitAsync(ct);
        try
        {
            ComprobarSesion(esperado);
            switch (accion)
            {
                case "Iniciar":
                    if (lecturas.Count == 24) throw new InvalidOperationException("El día ya terminó. Crea otro escenario para volver a iniciar.");
                    estado = "En ejecución";
                    break;
                case "Pausar":
                    if (estado == "En ejecución") estado = "Pausada";
                    break;
                case "Avanzar":
                    if (estado == "En ejecución") throw new InvalidOperationException("Pausa el reloj para avanzar una hora manualmente.");
                    if (lecturas.Count == 24) throw new InvalidOperationException("Ya se generaron las 24 horas del día.");
                    AvanzarHora();
                    if (estado != "Completada") estado = "Pausada";
                    break;
                case "Completar":
                    while (lecturas.Count < 24) AvanzarHora();
                    break;
                default: throw new ArgumentException("Acción de simulación no válida.", nameof(accion));
            }
            return Instantanea();
        }
        finally { mutex.Release(); }
    }

    private void AvanzarHora()
    {
        lecturas.Add(ModeloSolar.Calcular(configuracion, fecha, lecturas.Count));
        if (lecturas.Count == 24) estado = "Completada";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await mutex.WaitAsync(stoppingToken);
                try { if (estado == "En ejecución") AvanzarHora(); }
                finally { mutex.Release(); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    public async Task<EstadoSimulacion> GuardarAsync(int idUsuario, Guid? esperado, CancellationToken ct)
    {
        await mutex.WaitAsync(ct);
        try
        {
            ComprobarSesion(esperado);
            if (estado != "Completada") throw new InvalidOperationException("Completa las 24 horas antes de guardar las lecturas.");
            if (idSistemaGuardado.HasValue) return Instantanea();
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SolarGridDbContext>();
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);
            var comunidad = await db.Comunidades.SingleOrDefaultAsync(c => c.Nombre == "[SIM] Demostración El Salvador", ct);
            if (comunidad == null)
            {
                comunidad = new Comunidad { Nombre = "[SIM] Demostración El Salvador", Ubicacion = "El Salvador · ubicaciones aproximadas de simulación" };
                db.Comunidades.Add(comunidad);
            }
            var lugar = LugaresSimulacion.Obtener(configuracion.Ubicacion);
            var sistema = new SistemaSolar
            {
                Comunidad = comunidad,
                Nombre = $"[SIM] {lugar.Nombre} · {fecha:dd/MM/yyyy} · {id.ToString("N")[..8]}",
                FechaInstalacion = fecha,
                CapacidadKw = configuracion.PotenciaPanelW * configuracion.CantidadPaneles / 1000
            };
            db.SistemasSolares.Add(sistema);
            for (var numero = 1; numero <= configuracion.CantidadPaneles; numero++)
            {
                db.Equipos.Add(new Equipo
                {
                    Sistema = sistema, Codigo = $"SIM-{id:N}-{numero:D2}", Tipo = "Panel",
                    Marca = "Simulación SolarGrid", Modelo = $"Panel virtual {configuracion.PotenciaPanelW:0} W",
                    UbicacionPanel = new UbicacionPanel { Latitud = lugar.Latitud, Longitud = lugar.Longitud }
                });
            }
            db.LecturasEnergia.AddRange(lecturas.Select(l => new LecturaEnergia
            {
                Sistema = sistema, IdUsuario = idUsuario, FechaHora = l.Hora,
                GeneracionKwh = l.GeneracionKwh, ConsumoKwh = l.ConsumoKwh
            }));
            await db.SaveChangesAsync(ct);
            db.Historial.Add(new Historial
            {
                IdUsuario = idUsuario, Tabla = "SistemasSolares", IdRegistro = sistema.IdSistema, Accion = "Crear",
                Detalle = $"Simulación {id}: 24 lecturas; {configuracion.CantidadPaneles} panel(es) de {configuracion.PotenciaPanelW} W; clima {configuracion.Clima}; factor de consumo {configuracion.FactorConsumo}. Datos sintéticos."
            });
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            idSistemaGuardado = sistema.IdSistema;
            return Instantanea();
        }
        finally { mutex.Release(); }
    }
}
