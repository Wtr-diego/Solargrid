using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

[Authorize(Roles = "Administrador,Tecnico")]
public class ComunidadesController(SolarGridDbContext db, ILogger<ComunidadesController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new MapaComunidadesVm
        {
            Comunidades = await db.Comunidades.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(),
            // Se consultan TODOS los paneles, incluso los creados en versiones anteriores.
            Paneles = await db.Equipos.AsNoTracking()
                .Where(e => e.Tipo == "Panel")
                .OrderBy(e => e.Codigo)
                .Select(e => new PanelEnMapaVm
                {
                    IdEquipo = e.IdEquipo,
                    Codigo = e.Codigo,
                    Marca = e.Marca,
                    Modelo = e.Modelo,
                    Estado = e.Estado,
                    Sistema = e.Sistema.Nombre,
                    Comunidad = e.Sistema.Comunidad.Nombre,
                    IdComunidad = e.Sistema.IdComunidad,
                    Latitud = e.UbicacionPanel == null ? null : e.UbicacionPanel.Latitud,
                    Longitud = e.UbicacionPanel == null ? null : e.UbicacionPanel.Longitud
                }).ToListAsync()
        };
        return View(vm);
    }

    // Guarda la posición de un panel EXISTENTE, sin crear duplicados.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UbicarPanel(int idEquipo, double? latitud, double? longitud)
    {
        if (latitud is null || longitud is null ||
            !double.IsFinite(latitud.Value) || !double.IsFinite(longitud.Value) ||
            latitud < -90 || latitud > 90 || longitud < -180 || longitud > 180)
        {
            TempData["Error"] = "Selecciona primero una ubicación válida haciendo clic en el mapa.";
            return RedirectToAction(nameof(Index));
        }

        var equipo = await db.Equipos.Include(e => e.UbicacionPanel)
            .FirstOrDefaultAsync(e => e.IdEquipo == idEquipo && e.Tipo == "Panel");
        if (equipo == null)
        {
            TempData["Error"] = "El panel seleccionado no existe.";
            return RedirectToAction(nameof(Index));
        }
        if (equipo.UbicacionPanel == null)
        {
            db.UbicacionesPanel.Add(new UbicacionPanel
            {
                IdEquipo = equipo.IdEquipo,
                Latitud = latitud.Value,
                Longitud = longitud.Value,
                FechaActualizacion = DateTime.UtcNow
            });
        }
        else
        {
            equipo.UbicacionPanel.Latitud = latitud.Value;
            equipo.UbicacionPanel.Longitud = longitud.Value;
            equipo.UbicacionPanel.FechaActualizacion = DateTime.UtcNow;
        }

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error al guardar la ubicación del panel {IdEquipo}", idEquipo);
            TempData["Error"] = "No se pudo guardar la ubicación. Inténtalo nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] = $"Ubicación del panel {equipo.Codigo} guardada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarUbicacion(int idEquipo)
    {
        var panel = await db.Equipos.Include(e => e.UbicacionPanel)
            .FirstOrDefaultAsync(e => e.IdEquipo == idEquipo && e.Tipo == "Panel");
        if (panel == null)
        {
            TempData["Error"] = "El panel seleccionado no existe.";
            return RedirectToAction(nameof(Index));
        }
        if (panel.UbicacionPanel != null)
        {
            db.UbicacionesPanel.Remove(panel.UbicacionPanel);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error quitando la ubicación del panel {IdEquipo}", idEquipo);
                TempData["Error"] = "No se pudo quitar la ubicación.";
                return RedirectToAction(nameof(Index));
            }
        }
        TempData["Exito"] = $"Se quitó la ubicación del panel {panel.Codigo}. El equipo sigue registrado.";
        return RedirectToAction(nameof(Index));
    }

    private Task<EliminarComunidadVm?> ObtenerComunidadParaEliminar(int id) =>
        db.Comunidades.AsNoTracking().Where(c => c.IdComunidad == id)
            .Select(c => new EliminarComunidadVm
            {
                IdComunidad = c.IdComunidad,
                Nombre = c.Nombre,
                Ubicacion = c.Ubicacion,
                SistemasAsociados = db.SistemasSolares.Count(s => s.IdComunidad == c.IdComunidad),
                UsuariosAsociados = db.Usuarios.Count(u => u.IdComunidad == c.IdComunidad)
            }).SingleOrDefaultAsync();

    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var vm = await ObtenerComunidadParaEliminar(id);
        return vm == null ? NotFound() : View(vm);
    }

    [Authorize(Roles = "Administrador"), HttpPost, ActionName("Eliminar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        var vm = await ObtenerComunidadParaEliminar(id);
        if (vm == null) return NotFound();
        if (!vm.PuedeEliminar)
        {
            ModelState.AddModelError("", "No se puede eliminar una comunidad con sistemas o usuarios asociados. Reasígnalos primero o marca la comunidad como inactiva.");
            return View("Eliminar", vm);
        }

        db.Comunidades.Remove(new Comunidad { IdComunidad = id });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error al eliminar la comunidad {IdComunidad}", id);
            // Las claves foráneas también protegen frente a asociaciones creadas durante la confirmación.
            var actual = await ObtenerComunidadParaEliminar(id);
            if (actual == null) return NotFound();
            ModelState.AddModelError("", "No se pudo eliminar la comunidad. Revisa si tiene nuevos registros asociados e inténtalo de nuevo.");
            return View("Eliminar", actual);
        }
        TempData["Exito"] = $"Comunidad {vm.Nombre} eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador"), HttpGet]
    public IActionResult Crear() => View(new ComunidadVm());

    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ComunidadVm vm)
    {
        if (await db.Comunidades.AnyAsync(c => c.Nombre == vm.Nombre))
            ModelState.AddModelError(nameof(vm.Nombre), "Ya existe una comunidad con ese nombre.");
        if (!ModelState.IsValid) return View(vm);

        db.Comunidades.Add(new Comunidad { Nombre = vm.Nombre.Trim(), Ubicacion = vm.Ubicacion.Trim(), Estado = vm.Estado });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error al registrar una comunidad");
            ModelState.AddModelError("", "No se pudo guardar. Revisa si ya existe el registro.");
            return View(vm);
        }
        TempData["Exito"] = "Comunidad registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var c = await db.Comunidades.FindAsync(id);
        if (c == null) return NotFound();
        ViewBag.IdComunidad = id;
        return View(new ComunidadVm { Nombre = c.Nombre, Ubicacion = c.Ubicacion, Estado = c.Estado });
    }

    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ComunidadVm vm)
    {
        var c = await db.Comunidades.FindAsync(id);
        if (c == null) return NotFound();
        ViewBag.IdComunidad = id;
        if (await db.Comunidades.AnyAsync(x => x.Nombre == vm.Nombre && x.IdComunidad != id))
            ModelState.AddModelError(nameof(vm.Nombre), "Ya existe otra comunidad con ese nombre.");
        if (!ModelState.IsValid) return View(vm);
        c.Nombre = vm.Nombre.Trim();
        c.Ubicacion = vm.Ubicacion.Trim();
        c.Estado = vm.Estado;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error al editar una comunidad");
            ModelState.AddModelError("", "No se pudo actualizar el registro.");
            return View(vm);
        }
        TempData["Exito"] = "Comunidad actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
