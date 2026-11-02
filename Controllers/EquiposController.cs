using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

[Authorize(Roles = "Administrador,Tecnico")]
public class EquiposController(SolarGridDbContext db, ILogger<EquiposController> logger) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Equipos.Include(x => x.Sistema).ThenInclude(s => s.Comunidad)
        .OrderBy(x => x.Codigo).ToListAsync());

    private async Task CargarSistemas() => ViewBag.Sistemas = new SelectList(
        await db.SistemasSolares.OrderBy(s => s.Nombre).ToListAsync(), "IdSistema", "Nombre");

    [HttpGet]
    public async Task<IActionResult> Crear() { await CargarSistemas(); return View(new EquipoVm()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(EquipoVm vm)
    {
        if (!await db.SistemasSolares.AnyAsync(s => s.IdSistema == vm.IdSistema))
            ModelState.AddModelError(nameof(vm.IdSistema), "Selecciona un sistema existente.");
        if (await db.Equipos.AnyAsync(e => e.Codigo == vm.Codigo.Trim()))
            ModelState.AddModelError(nameof(vm.Codigo), "Ya existe un equipo con ese código.");
        if (!ModelState.IsValid) { await CargarSistemas(); return View(vm); }
        db.Equipos.Add(new Equipo { IdSistema = vm.IdSistema, Codigo = vm.Codigo.Trim(), Tipo = vm.Tipo,
            Marca = vm.Marca.Trim(), Modelo = vm.Modelo.Trim(), Estado = vm.Estado });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error guardando equipo");
            ModelState.AddModelError("", "No se pudo guardar. Revisa que el código no esté repetido.");
            await CargarSistemas(); return View(vm);
        }
        TempData["Exito"] = "Equipo registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var e = await db.Equipos.FindAsync(id);
        if (e == null) return NotFound();
        ViewBag.IdEquipo = id;
        await CargarSistemas();
        return View(new EquipoVm { IdSistema = e.IdSistema, Codigo = e.Codigo, Tipo = e.Tipo,
            Marca = e.Marca, Modelo = e.Modelo, Estado = e.Estado });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, EquipoVm vm)
    {
        var e = await db.Equipos.FindAsync(id);
        if (e == null) return NotFound();
        ViewBag.IdEquipo = id;
        if (!await db.SistemasSolares.AnyAsync(s => s.IdSistema == vm.IdSistema))
            ModelState.AddModelError(nameof(vm.IdSistema), "Selecciona un sistema existente.");
        if (await db.Equipos.AnyAsync(x => x.Codigo == vm.Codigo.Trim() && x.IdEquipo != id))
            ModelState.AddModelError(nameof(vm.Codigo), "Este código ya pertenece a otro equipo.");
        if (!ModelState.IsValid) { await CargarSistemas(); return View(vm); }
        e.IdSistema = vm.IdSistema; e.Codigo = vm.Codigo.Trim(); e.Tipo = vm.Tipo;
        e.Marca = vm.Marca.Trim(); e.Modelo = vm.Modelo.Trim(); e.Estado = vm.Estado;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error editando equipo");
            ModelState.AddModelError("", "No se pudo actualizar el equipo.");
            await CargarSistemas(); return View(vm);
        }
        TempData["Exito"] = "Equipo actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
