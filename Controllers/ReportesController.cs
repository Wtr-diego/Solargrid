using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

[Authorize(Roles = "Administrador,Tecnico")]
public class ReportesController(SolarGridDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, int? idSistema)
    {
        var inicio = (desde ?? DateTime.Today.AddDays(-30)).Date;
        var fin = (hasta ?? DateTime.Today).Date;
        ViewBag.Desde = inicio.ToString("yyyy-MM-dd");
        ViewBag.Hasta = fin.ToString("yyyy-MM-dd");
        ViewBag.IdSistema = idSistema;
        ViewBag.Sistemas = new SelectList(await db.SistemasSolares.OrderBy(s => s.Nombre).ToListAsync(), "IdSistema", "Nombre", idSistema);
        if (fin < inicio)
        {
            ViewBag.Error = "La fecha inicial no puede ser posterior a la fecha final.";
            return View(new List<SolarGrid.Models.LecturaEnergia>());
        }
        if (idSistema.HasValue && !await db.SistemasSolares.AnyAsync(s => s.IdSistema == idSistema.Value))
        {
            ViewBag.Error = "El sistema seleccionado no existe.";
            return View(new List<SolarGrid.Models.LecturaEnergia>());
        }
        var consulta = db.LecturasEnergia.Include(l => l.Sistema)
            .Where(l => l.FechaHora >= inicio && l.FechaHora < fin.AddDays(1));
        if (idSistema.HasValue) consulta = consulta.Where(l => l.IdSistema == idSistema.Value);
        ViewBag.Generacion = await consulta.SumAsync(x => (decimal?)x.GeneracionKwh) ?? 0m;
        ViewBag.Consumo = await consulta.SumAsync(x => (decimal?)x.ConsumoKwh) ?? 0m;
        ViewBag.AlertasConsumo = await consulta
            .GroupBy(l => new { l.IdSistema, l.Sistema.Nombre, Comunidad = l.Sistema.Comunidad.Nombre })
            .Select(g => new AlertaConsumoVm
            {
                Comunidad = g.Key.Comunidad,
                Sistema = g.Key.Nombre,
                GeneracionKwh = g.Sum(l => l.GeneracionKwh),
                ConsumoKwh = g.Sum(l => l.ConsumoKwh)
            })
            .Where(a => a.ConsumoKwh > a.GeneracionKwh)
            .OrderByDescending(a => a.ConsumoKwh - a.GeneracionKwh)
            .ToListAsync();
        ViewBag.Total = await consulta.CountAsync();
        var datos = await consulta.OrderByDescending(l => l.FechaHora).Take(500).ToListAsync();
        return View(datos);
    }
}
