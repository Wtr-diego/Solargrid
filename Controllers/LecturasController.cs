using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

[Authorize(Roles = "Administrador,Tecnico")]
public class LecturasController(SolarGridDbContext db, ILogger<LecturasController> logger) : Controller
{
    public async Task<IActionResult> Index() => View(await db.LecturasEnergia
        .Include(l => l.Sistema).Include(l => l.Usuario)
        .OrderByDescending(l => l.FechaHora).Take(100).ToListAsync());

    private async Task CargarSistemas() => ViewBag.Sistemas = new SelectList(
        await db.SistemasSolares.OrderBy(s => s.Nombre).ToListAsync(), "IdSistema", "Nombre");

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarSistemas();
        return View(new LecturaVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(LecturaVm vm)
    {
        if (vm.FechaHora > DateTime.Now)
            ModelState.AddModelError(nameof(vm.FechaHora), "No se permiten fechas futuras.");
        if (!await db.SistemasSolares.AnyAsync(s => s.IdSistema == vm.IdSistema))
            ModelState.AddModelError(nameof(vm.IdSistema), "Selecciona un sistema registrado.");
        if (!ModelState.IsValid)
        {
            await CargarSistemas();
            return View(vm);
        }
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        db.LecturasEnergia.Add(new LecturaEnergia
        {
            IdSistema = vm.IdSistema,
            IdUsuario = idUsuario,
            FechaHora = vm.FechaHora,
            GeneracionKwh = vm.GeneracionKwh,
            ConsumoKwh = vm.ConsumoKwh
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error al guardar lectura de energía");
            ModelState.AddModelError("", "No se pudo guardar la lectura.");
            await CargarSistemas();
            return View(vm);
        }
        TempData["Exito"] = "Lectura de energía registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
