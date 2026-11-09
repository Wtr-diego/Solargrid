using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Simulacion;

namespace SolarGrid.Controllers;

[Authorize(Roles = "Administrador,Tecnico")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SimulacionController(SimuladorSolar simulador, ILogger<SimulacionController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await simulador.ObtenerAsync(ct));

    [HttpGet]
    public async Task<IActionResult> Estado(CancellationToken ct) => Json(await simulador.ObtenerAsync(ct));

    private async Task<IActionResult> Responder(Func<Task<EstadoSimulacion>> accion)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Revisa los valores ingresados." : e.ErrorMessage)) });
        try { return Json(await accion()); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Configurar(ConfiguracionSimulacion configuracion, Guid? id, CancellationToken ct)
        => Responder(() => simulador.ConfigurarAsync(configuracion, id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Iniciar(Guid? id, CancellationToken ct) => Responder(() => simulador.EjecutarAsync("Iniciar", id, ct));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Pausar(Guid? id, CancellationToken ct) => Responder(() => simulador.EjecutarAsync("Pausar", id, ct));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Avanzar(Guid? id, CancellationToken ct) => Responder(() => simulador.EjecutarAsync("Avanzar", id, ct));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Completar(Guid? id, CancellationToken ct) => Responder(() => simulador.EjecutarAsync("Completar", id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Guardar(Guid? id, CancellationToken ct) => Responder(async () =>
    {
        try { return await simulador.GuardarAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, ct); }
        catch (Exception ex) when (ex is DbUpdateException or SqlException)
        {
            logger.LogError(ex, "No se pudo guardar el día simulado");
            throw new InvalidOperationException("No se pudieron guardar las lecturas. Revisa la conexión a la base de datos y vuelve a intentarlo. La simulación sigue disponible.");
        }
    });

    [HttpGet]
    public async Task<IActionResult> Exportar(CancellationToken ct)
    {
        var datos = await simulador.ObtenerAsync(ct);
        var csv = new StringBuilder("Origen,Simulacion,Ubicacion,Latitud,Longitud,Clima,Paneles,Potencia_panel_W,Factor_consumo,Inicio_hora_local_UTC-6,Irradiancia_W_m2,Temperatura_ambiente_C,Temperatura_panel_C,Potencia_solar_W,Potencia_consumo_W,Generacion_kWh,Consumo_kWh,Autoconsumo_solar_kWh,Importacion_red_kWh,Excedente_kWh\r\n");
        foreach (var l in datos.Lecturas)
        {
            csv.AppendLine(string.Join(",", new object[]
            {
                "SIMULADO", datos.Id, datos.Lugar.Nombre, datos.Lugar.Latitud, datos.Lugar.Longitud,
                datos.Configuracion.Clima, datos.Configuracion.CantidadPaneles, datos.Configuracion.PotenciaPanelW,
                datos.Configuracion.FactorConsumo, l.Hora.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                l.IrradianciaWm2, l.TemperaturaAmbienteC, l.TemperaturaPanelC, l.PotenciaSolarW, l.PotenciaConsumoW,
                l.GeneracionKwh, l.ConsumoKwh, l.AutoconsumoKwh, l.ImportacionKwh, l.ExcedenteKwh
            }.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
        }
        var utf8 = new UTF8Encoding(true);
        return File(utf8.GetPreamble().Concat(utf8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", $"SolarGrid-simulacion-{datos.Fecha:yyyy-MM-dd}-{datos.Id.ToString("N")[..8]}.csv");
    }
}
