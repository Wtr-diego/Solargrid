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
}
