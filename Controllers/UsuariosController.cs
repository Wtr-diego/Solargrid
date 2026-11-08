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
    [HttpGet]
    public async Task<IActionResult> Index() => View(await db.Usuarios.AsNoTracking()
        .Include(u => u.Comunidad)
        .OrderBy(u => u.Nombre).ThenBy(u => u.Correo)
        .ToListAsync());

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
        db.Usuarios.Add(usuario);

        if (!await GuardarCambios()) return await MostrarFormulario(vm);

        TempData["Exito"] = "Usuario registrado correctamente.";
        return RedirectToAction(nameof(Index));
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
        if (vm.Activo is null)
            ModelState.AddModelError(nameof(vm.Activo), "Selecciona el estado de la cuenta.");

        var idAdministrador = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == idAdministrador)
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
            if (!await db.Usuarios.AnyAsync(u => u.IdUsuario == idAdministrador && u.Activo && u.Rol == "Administrador"))
                return Forbid();

            var usuario = await db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Nombre = vm.Nombre.Trim();
            usuario.Correo = correo;
            usuario.Rol = vm.Rol;
            usuario.IdComunidad = vm.IdComunidad;
            usuario.Activo = vm.Activo!.Value;

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
}
