using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;
using SolarGrid.ViewModels;

namespace SolarGrid.Controllers;

public class CuentaController(SolarGridDbContext db, IPasswordHasher<Usuario> hasher, ILogger<CuentaController> logger) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Ingresar(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View(new IngresoVm());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(IngresoVm vm, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var correo = vm.Correo.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo.ToLower() == correo && u.Activo);
        var esValido = usuario != null &&
                       hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, vm.Contrasena) != PasswordVerificationResult.Failed;
        if (!esValido)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario!.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol),
            new(SelloSesion.Claim, SelloSesion.Calcular(usuario.PasswordHash))
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Ingresar));
    }

    [AllowAnonymous]
    public IActionResult SinPermiso() => View();

    [AllowAnonymous, HttpGet]
    public IActionResult OlvideContrasena() => View(new SolicitudRecuperacionVm());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OlvideContrasena(SolicitudRecuperacionVm vm)
    {
        var digitos = vm.Telefono?.Count(char.IsAsciiDigit) ?? 0;
        if (digitos is < 8 or > 15)
            ModelState.AddModelError(nameof(vm.Telefono), "El número debe tener entre 8 y 15 dígitos.");
        if (!ModelState.IsValid) return View(vm);

        var correo = vm.Correo.Trim().ToLowerInvariant();
        try
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var usuario = await db.Usuarios.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Activo && u.Correo.ToLower() == correo);
            if (usuario != null)
            {
                var limite = DateTime.Now.AddMinutes(-15);
                if (!await db.Historial.AnyAsync(h => h.Tabla == "Recuperacion" &&
                    h.IdUsuario == usuario.IdUsuario && h.FechaHora >= limite))
                {
                    db.Historial.Add(new Historial
                    {
                        IdUsuario = usuario.IdUsuario,
                        IdRegistro = usuario.IdUsuario,
                        Tabla = "Recuperacion",
                        Accion = "Solicitud",
                        Detalle = vm.Telefono!.Trim()
                    });
                    await db.SaveChangesAsync();
                }
            }
            await transaccion.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or System.Data.Common.DbException)
        {
            logger.LogWarning(ex, "No se pudo procesar una solicitud de recuperación.");
        }

        TempData["Exito"] = "Si el correo corresponde a una cuenta activa, el administrador podrá revisar tu solicitud. Si necesitas ayuda, contáctalo directamente.";
        return RedirectToAction(nameof(OlvideContrasena));
    }

    [Authorize, HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Presencia() => NoContent();
}
