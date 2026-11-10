using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Data;
using SolarGrid.Models;
using SolarGrid.Simulacion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PresenciaUsuarios>();
builder.Services.AddSingleton<SimuladorSolar>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<SimuladorSolar>());
builder.Services.AddDbContext<SolarGridDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SolarGrid")));
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Ingresar";
        options.AccessDeniedPath = "/Cuenta/SinPermiso";
        options.Cookie.Name = "SolarGrid.Sesion";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            if (principal != null && int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<SolarGridDbContext>();
                var usuario = await db.Usuarios
                    .Where(u => u.IdUsuario == id && u.Activo)
                    .Select(u => new { u.Nombre, u.Rol, u.PasswordHash })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                if (usuario != null && principal.IsInRole(usuario.Rol)
                    && principal.FindFirstValue(SelloSesion.Claim) == SelloSesion.Calcular(usuario.PasswordHash))
                {
                    if (principal.Identity is ClaimsIdentity identity &&
                        identity.FindFirst(ClaimTypes.Name) is Claim nombre && nombre.Value != usuario.Nombre)
                    {
                        var actualizado = new ClaimsIdentity(identity);
                        actualizado.RemoveClaim(actualizado.FindFirst(ClaimTypes.Name)!);
                        actualizado.AddClaim(new Claim(ClaimTypes.Name, usuario.Nombre));
                        context.ReplacePrincipal(new ClaimsPrincipal(actualizado));
                        context.ShouldRenew = true;
                    }

                    context.HttpContext.RequestServices.GetRequiredService<PresenciaUsuarios>().Registrar(id);
                    return;
                }
            }

            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Solo para la demostración local. Para producción se usarán migraciones EF Core.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DatosIniciales.CrearAsync(scope.ServiceProvider);
}

app.Run();
