using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

[Authorize(Roles = "Administrador,Tecnico")]
public class SistemasController(SolarGridDbContext db, ILogger<SistemasController> logger) : Controller
{
    public async Task<IActionResult> Index() => View(await db.SistemasSolares.Include(s => s.Comunidad).OrderBy(s => s.Nombre).ToListAsync());

    private async Task CargarComunidades() => ViewBag.Comunidades = new SelectList(
        await db.Comunidades.Where(c => c.Estado == "Activa").OrderBy(c => c.Nombre).ToListAsync(),
        "IdComunidad", "Nombre");

    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarComunidades();
        return View(new SistemaVm());
    }

    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(SistemaVm vm)
    {
        if (!await db.Comunidades.AnyAsync(c => c.IdComunidad == vm.IdComunidad && c.Estado == "Activa"))
            ModelState.AddModelError(nameof(vm.IdComunidad), "Selecciona una comunidad activa registrada.");
        if (!ModelState.IsValid)
        {
            await CargarComunidades();
            return View(vm);
        }
        db.SistemasSolares.Add(new SistemaSolar
        {
            IdComunidad = vm.IdComunidad,
            Nombre = vm.Nombre.Trim(),
            FechaInstalacion = vm.FechaInstalacion,
            CapacidadKw = vm.CapacidadKw,
            Estado = vm.Estado
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error al guardar sistema solar");
            ModelState.AddModelError("", "No se pudo guardar el sistema solar.");
            await CargarComunidades();
            return View(vm);
        }
        TempData["Exito"] = "Sistema solar registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }
    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var registro = await db.SistemasSolares.FindAsync(id);
        if (registro == null) return NotFound();

        var resumen = await ObtenerResumen(id);
        var bloqueo = resumen.Equipos > 0
            ? $"Retira primero los equipos asociados ({resumen.Equipos})."
            : null;
        return View("ConfirmarEliminacion", new ConfirmarEliminacionVm(
            id, "sistema solar", registro.Nombre, bloqueo,
            $"Junto con el sistema se borrarán {resumen.Lecturas} lecturas de energía, "
            + $"{resumen.Incidencias} incidencias y {resumen.Reportes} reportes.",
            resumen.Confirmacion));
    }

    [Authorize(Roles = "Administrador"), HttpPost, ActionName("Eliminar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id, string? confirmacion)
    {
        try
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var registro = await db.SistemasSolares.FindAsync(id);
            if (registro == null) return NotFound();

            var resumen = await ObtenerResumen(id);
            if (resumen.Equipos > 0 || confirmacion != resumen.Confirmacion)
            {
                TempData["Error"] = "Revisa los registros asociados y confirma nuevamente.";
                return RedirectToAction(nameof(Eliminar), new { id });
            }

            await db.Reportes.Where(x => x.IdSistema == id).ExecuteDeleteAsync();
            await db.Incidencias.Where(x => x.IdSistema == id).ExecuteDeleteAsync();
            await db.LecturasEnergia.Where(x => x.IdSistema == id).ExecuteDeleteAsync();
            db.SistemasSolares.Remove(registro);
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or System.Data.Common.DbException)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<SistemasController>>()
                .LogError(ex, "Error al eliminar el sistema {Id}", id);
            TempData["Error"] = "No se pudo eliminar el sistema. Revisa los registros asociados e intenta nuevamente.";
            return RedirectToAction(nameof(Eliminar), new { id });
        }

        TempData["Exito"] = "Se eliminó el sistema solar y sus registros asociados.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ResumenEliminacion> ObtenerResumen(int id) => new(
        await db.Equipos.CountAsync(x => x.IdSistema == id),
        await db.LecturasEnergia.CountAsync(x => x.IdSistema == id),
        await db.Incidencias.CountAsync(x => x.IdSistema == id),
        await db.Reportes.CountAsync(x => x.IdSistema == id));

    private sealed record ResumenEliminacion(int Equipos, int Lecturas, int Incidencias, int Reportes)
    {
        public string Confirmacion => $"{Lecturas}:{Incidencias}:{Reportes}";
    }
}
