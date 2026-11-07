using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

[Authorize]
public class HomeController(SolarGridDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Administrador") || User.IsInRole("Tecnico"))
        {
            var inicioPeriodo = DateTime.Today.AddDays(-29);
            var finPeriodo = DateTime.Today.AddDays(1);
            ViewBag.InicioPeriodoAlerta = inicioPeriodo.ToString("dd/MM/yyyy");
            ViewBag.FinPeriodoAlerta = DateTime.Today.ToString("dd/MM/yyyy");
            ViewBag.AlertasConsumo = await db.LecturasEnergia
                .Where(l => l.FechaHora >= inicioPeriodo && l.FechaHora < finPeriodo)
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
            ViewBag.TotalComunidades = await db.Comunidades.CountAsync();
            ViewBag.TotalSistemas = await db.SistemasSolares.CountAsync();
            ViewBag.TotalEquipos = await db.Equipos.CountAsync();
            ViewBag.TotalLecturas = await db.LecturasEnergia.CountAsync();
            ViewBag.IncidenciasAbiertas = await db.Incidencias.CountAsync(i => i.Estado == "Abierta" || i.Estado == "En proceso");
            ViewBag.UltimasLecturas = await db.LecturasEnergia.Include(l => l.Sistema)
                .OrderByDescending(l => l.FechaHora).Take(5).ToListAsync();
        }
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
