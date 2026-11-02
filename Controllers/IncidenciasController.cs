using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

[Authorize]
public class IncidenciasController(SolarGridDbContext db, ILogger<IncidenciasController> logger) : Controller
{
    private int IdUsuario => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Personal => User.IsInRole("Administrador") || User.IsInRole("Tecnico");

    private async Task<int?> IdComunidadAsync() => await db.Usuarios
        .Where(u => u.IdUsuario == IdUsuario).Select(u => u.IdComunidad).SingleOrDefaultAsync();

    public async Task<IActionResult> Index()
    {
        var consulta = db.Incidencias.Include(i => i.Sistema).Include(i => i.Equipo).Include(i => i.Reporta).AsQueryable();
        if (!Personal)
        {
            var idComunidad = await IdComunidadAsync();
            consulta = consulta.Where(i => i.Sistema.IdComunidad == idComunidad);
        }
        return View(await consulta.OrderByDescending(i => i.FechaCreacion).ToListAsync());
    }

    private async Task CargarOpciones()
    {
        var sistemas = db.SistemasSolares.AsQueryable();
        if (!Personal)
        {
            var idComunidad = await IdComunidadAsync();
            sistemas = sistemas.Where(s => s.IdComunidad == idComunidad);
        }
        ViewBag.Sistemas = new SelectList(await sistemas.OrderBy(s => s.Nombre).ToListAsync(), "IdSistema", "Nombre");
        var ids = await sistemas.Select(s => s.IdSistema).ToListAsync();
        ViewBag.Equipos = new SelectList(await db.Equipos.Where(e => ids.Contains(e.IdSistema)).OrderBy(e => e.Codigo).ToListAsync(), "IdEquipo", "Codigo");
    }

    [HttpGet]
    public async Task<IActionResult> Crear() { await CargarOpciones(); return View(new IncidenciaVm()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(IncidenciaVm vm)
    {
        var sistema = await db.SistemasSolares.FindAsync(vm.IdSistema);
        if (sistema == null || (!Personal && sistema.IdComunidad != await IdComunidadAsync()))
            ModelState.AddModelError(nameof(vm.IdSistema), "Selecciona un sistema permitido.");
        if (vm.IdEquipo.HasValue && !await db.Equipos.AnyAsync(e => e.IdEquipo == vm.IdEquipo.Value && e.IdSistema == vm.IdSistema))
            ModelState.AddModelError(nameof(vm.IdEquipo), "El equipo debe pertenecer al sistema seleccionado.");
        if (!ModelState.IsValid) { await CargarOpciones(); return View(vm); }
        db.Incidencias.Add(new Incidencia { IdSistema = vm.IdSistema, IdEquipo = vm.IdEquipo,
            IdReporta = IdUsuario, Descripcion = vm.Descripcion.Trim(), Prioridad = vm.Prioridad,
            Estado = "Abierta", FechaCreacion = DateTime.Now });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error registrando incidencia");
            ModelState.AddModelError("", "No se pudo registrar el problema.");
            await CargarOpciones(); return View(vm);
        }
        TempData["Exito"] = "Problema reportado correctamente (estado: Abierta).";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Tecnico"), HttpGet]
    public async Task<IActionResult> Estado(int id)
    {
        var i = await db.Incidencias.Include(i => i.Sistema).FirstOrDefaultAsync(i => i.IdIncidencia == id);
        if (i == null) return NotFound();
        ViewBag.Incidencia = i;
        return View(new EstadoIncidenciaVm { Estado = i.Estado, Solucion = i.Solucion });
    }

    [Authorize(Roles = "Administrador,Tecnico"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Estado(int id, EstadoIncidenciaVm vm)
    {
        var i = await db.Incidencias.Include(i => i.Sistema).FirstOrDefaultAsync(i => i.IdIncidencia == id);
        if (i == null) return NotFound();
        ViewBag.Incidencia = i;
        if ((vm.Estado is "Resuelta" or "Cerrada") && string.IsNullOrWhiteSpace(vm.Solucion))
            ModelState.AddModelError(nameof(vm.Solucion), "Escribe la solución antes de resolver o cerrar.");
        if (!ModelState.IsValid) return View(vm);
        i.Estado = vm.Estado; i.Solucion = vm.Solucion?.Trim();
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error actualizando incidencia");
            ModelState.AddModelError("", "No se pudo actualizar el problema.");
            return View(vm);
        }
        TempData["Exito"] = "Estado de incidencia actualizado.";
        return RedirectToAction(nameof(Index));
    }
}
