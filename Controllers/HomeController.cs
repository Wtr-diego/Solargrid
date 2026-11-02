using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;

namespace SolarGrid.Controllers;

[Authorize]
public class HomeController(SolarGridDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Administrador") || User.IsInRole("Tecnico"))
        {
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
