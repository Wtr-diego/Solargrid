using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController(
    SolarGridDbContext db,
    IPasswordHasher<Usuario> hasher,
    ILogger<UsuariosController> logger) : Controller
{
    private int IdAdministrador => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Presencia([FromServices] PresenciaUsuarios presencia)
    {
        var activos = await db.Usuarios.Where(u => u.Activo).Select(u => u.IdUsuario).ToListAsync();
        return Json(activos.Where(presencia.EnLinea).ToArray());
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await db.Usuarios.AsNoTracking()
        .Include(u => u.Comunidad)
        .OrderBy(u => u.Nombre).ThenBy(u => u.Correo)
        .ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Historial(int? antesDe)
    {
        const int tamanoPagina = 50;
        var movimientos = await db.Historial.AsNoTracking()
            .Where(h => h.Tabla == nameof(SolarGridDbContext.Usuarios) &&
                        (!antesDe.HasValue || h.IdHistorial < antesDe.Value))
            .OrderByDescending(h => h.IdHistorial)
            .Select(h => new HistorialUsuarioVm(
                h.IdHistorial, h.FechaHora, h.IdUsuario, h.Usuario.Nombre, h.IdRegistro,
                db.Usuarios.Where(u => u.IdUsuario == h.IdRegistro).Select(u => u.Nombre).FirstOrDefault(),
                h.Accion, h.Detalle))
            .Take(tamanoPagina + 1)
            .ToListAsync();

        ViewBag.PaginaAnterior = antesDe.HasValue;
        if (movimientos.Count > tamanoPagina)
        {
            movimientos.RemoveAt(tamanoPagina);
            ViewBag.AntesDe = movimientos[^1].IdHistorial;
        }

        return View(movimientos);
    }

    [HttpGet]
    public Task<IActionResult> Crear() => MostrarFormulario(new UsuarioVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(UsuarioVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Contrasena))
            ModelState.AddModelError(nameof(vm.Contrasena), "Escribe una contraseña inicial.");

        var correo = await ValidarDatos(vm);
        if (!ModelState.IsValid) return await MostrarFormulario(vm);

        var usuario = new Usuario
        {
            Nombre = vm.Nombre.Trim(),
            Correo = correo,
            Rol = vm.Rol,
            IdComunidad = vm.IdComunidad,
            Activo = true
        };
        usuario.PasswordHash = hasher.HashPassword(usuario, vm.Contrasena!);
        try
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await AdministradorSigueActivo()) return Forbid();

            db.Usuarios.Add(usuario);
            if (await GuardarCambios())
            {
                RegistrarHistorial(usuario.IdUsuario, "Creación", $"Cuenta activa con rol {MostrarRol(usuario.Rol)}.");
                if (await GuardarCambios())
                {
                    await transaccion.CommitAsync();
                    TempData["Exito"] = "Usuario registrado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
            }
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            logger.LogWarning(ex, "Conflicto al registrar un usuario");
            ModelState.AddModelError("", "Hubo un cambio simultáneo. Intenta guardar nuevamente.");
        }

        return await MostrarFormulario(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.IdUsuario == id);
        if (usuario == null) return NotFound();

        return await MostrarFormulario(new UsuarioVm
        {
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            Rol = usuario.Rol,
            IdComunidad = usuario.IdComunidad,
            Activo = usuario.Activo
        }, id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, UsuarioVm vm)
    {
        if (vm.Contrasena != null && string.IsNullOrWhiteSpace(vm.Contrasena))
            ModelState.AddModelError(nameof(vm.Contrasena), "Escribe una contraseña válida o deja el campo vacío.");

        if (vm.Activo is null)
            ModelState.AddModelError(nameof(vm.Activo), "Selecciona el estado de la cuenta.");

        if (id == IdAdministrador)
        {
            if (vm.Rol != "Administrador")
                ModelState.AddModelError(nameof(vm.Rol), "No puedes cambiar el rol de tu propia cuenta.");
            if (vm.Activo == false)
                ModelState.AddModelError(nameof(vm.Activo), "No puedes desactivar tu propia cuenta.");
        }

        var correo = await ValidarDatos(vm, id);
        if (!ModelState.IsValid) return await MostrarFormulario(vm, id);

        try
        {
            // Conserva el permiso del administrador hasta guardar, incluso si dos administradores se editan entre sí.
            await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await AdministradorSigueActivo()) return Forbid();

            var usuario = await db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            var nombre = vm.Nombre.Trim();
            var activo = vm.Activo!.Value;
            var cambios = new List<string>();
            if (usuario.Nombre != nombre) cambios.Add("Nombre");
            if (usuario.Correo != correo) cambios.Add("Correo");
            if (usuario.Rol != vm.Rol) cambios.Add($"Rol de {MostrarRol(usuario.Rol)} a {MostrarRol(vm.Rol)}");
            if (usuario.IdComunidad != vm.IdComunidad) cambios.Add("Comunidad asignada");
            if (vm.Contrasena is { Length: > 0 } contrasena)
            {
                usuario.PasswordHash = hasher.HashPassword(usuario, contrasena);
                cambios.Add("Restablecimiento de contraseña");
            }

            var accion = "Edición";
            if (usuario.Activo != activo)
            {
                accion = activo ? "Reactivación" : "Desactivación";
                cambios.Add(activo ? "Estado de Inactivo a Activo" : "Estado de Activo a Inactivo");
            }

            if (cambios.Count > 0) RegistrarHistorial(id, accion, string.Join(", ", cambios) + ".");

            usuario.Nombre = nombre;
            usuario.Correo = correo;
            usuario.Rol = vm.Rol;
            usuario.IdComunidad = vm.IdComunidad;
            usuario.Activo = activo;

            if (await GuardarCambios())
            {
                await transaccion.CommitAsync();
                TempData["Exito"] = "Usuario actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            logger.LogWarning(ex, "Conflicto al editar el usuario {IdUsuario}", id);
            ModelState.AddModelError("", "Hubo un cambio simultáneo. Intenta guardar nuevamente.");
        }

        return await MostrarFormulario(vm, id);
    }

    private Task<bool> AdministradorSigueActivo()
    {
        var id = IdAdministrador;
        return db.Usuarios.AnyAsync(u => u.IdUsuario == id && u.Activo && u.Rol == "Administrador");
    }

    private static string MostrarRol(string rol) => rol == "Tecnico" ? "Técnico" : rol;

    private void RegistrarHistorial(int idUsuario, string accion, string detalle)
    {
        db.Historial.Add(new Historial
        {
            IdUsuario = IdAdministrador,
            Tabla = nameof(SolarGridDbContext.Usuarios),
            IdRegistro = idUsuario,
            Accion = accion,
            Detalle = detalle
        });
    }

    private async Task<string> ValidarDatos(UsuarioVm vm, int? id = null)
    {
        if (vm.IdComunidad is int idComunidad)
        {
            if (!await db.Comunidades.AnyAsync(c => c.IdComunidad == idComunidad))
                ModelState.AddModelError(nameof(vm.IdComunidad), "Selecciona una comunidad existente.");
        }
        else if (vm.Rol == "Comunidad")
        {
            ModelState.AddModelError(nameof(vm.IdComunidad), "Selecciona una comunidad para este rol.");
        }

        var correo = vm.Correo?.Trim().ToLowerInvariant() ?? "";
        if (ModelState.IsValid && await db.Usuarios.AnyAsync(u => u.IdUsuario != id && u.Correo.ToLower() == correo))
            ModelState.AddModelError(nameof(vm.Correo), "Ya existe un usuario con ese correo.");

        return correo;
    }

    private async Task<bool> GuardarCambios()
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(nameof(UsuarioVm.Correo), "Ya existe un usuario con ese correo.");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error al guardar un usuario");
            ModelState.AddModelError("", "No se pudo guardar el usuario. Inténtalo nuevamente.");
        }

        return false;
    }

    private async Task<IActionResult> MostrarFormulario(UsuarioVm vm, int? id = null)
    {
        var comunidades = await db.Comunidades.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
        ViewBag.Comunidades = new SelectList(comunidades, "IdComunidad", "Nombre");
        ViewBag.IdUsuario = id;
        return View("Formulario", vm);
    }
    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var registro = await db.Usuarios.FindAsync(id);
        if (registro == null) return NotFound();

        return View("ConfirmarEliminacion", new ConfirmarEliminacionVm(
            id, "usuario", registro.Nombre, await MotivoBloqueo(id)));
    }

    [Authorize(Roles = "Administrador"), HttpPost, ActionName("Eliminar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        if (!await AdministradorSigueActivo()) return Forbid();
        var registro = await db.Usuarios.FindAsync(id);
        if (registro == null) return NotFound();

        var motivo = await MotivoBloqueo(id);
        if (motivo != null)
        {
            TempData["Error"] = motivo;
            return RedirectToAction(nameof(Eliminar), new { id });
        }

        db.Usuarios.Remove(registro);
        RegistrarHistorial(id, "Eliminación", $"Se eliminó la cuenta {registro.Nombre}.");
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<UsuariosController>>()
                .LogError(ex, "Error al eliminar el registro {Id}", id);
            TempData["Error"] = "No se pudo eliminar el registro. Revisa sus relaciones e inténtalo de nuevo.";
            return RedirectToAction(nameof(Eliminar), new { id });
        }

        TempData["Exito"] = "Se eliminó el usuario correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> MotivoBloqueo(int id)
    {
        if (id == IdAdministrador) return "No puedes eliminar tu propia cuenta.";
        if (await db.LecturasEnergia.AnyAsync(x => x.IdUsuario == id)
            || await db.Incidencias.AnyAsync(x => x.IdReporta == id)
            || await db.Reportes.AnyAsync(x => x.IdUsuario == id)
            || await db.Historial.AnyAsync(x => x.IdUsuario == id))
            return "La cuenta tiene registros asociados. Puedes desactivarla desde Editar.";

        return null;
    }
}
