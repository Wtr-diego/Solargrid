using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SolarGrid.Models;

namespace SolarGrid.Data;

public static class DatosIniciales
{
    public static async Task CrearAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<SolarGridDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<Usuario>>();
        await db.Database.EnsureCreatedAsync();
        // EnsureCreated no actualiza bases que ya existían. Esta tabla nueva se agrega sin
        // borrar comunidades, sistemas, lecturas ni equipos de la versión anterior.
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.UbicacionesPanel', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.UbicacionesPanel (
                    IdEquipo INT NOT NULL CONSTRAINT PK_UbicacionesPanel PRIMARY KEY,
                    Latitud FLOAT NOT NULL,
                    Longitud FLOAT NOT NULL,
                    FechaActualizacion DATETIME2 NOT NULL,
                    CONSTRAINT FK_UbicacionesPanel_Equipos_IdEquipo FOREIGN KEY (IdEquipo)
                        REFERENCES dbo.Equipos (IdEquipo) ON DELETE CASCADE,
                    CONSTRAINT CK_Ubicacion_Latitud CHECK (Latitud BETWEEN -90 AND 90),
                    CONSTRAINT CK_Ubicacion_Longitud CHECK (Longitud BETWEEN -180 AND 180)
                );
            END
            """);
        if (await db.Usuarios.AnyAsync()) return; // No modificar datos ya registrados.

        var c1 = new Comunidad { Nombre = "Comunidad El Pinar", Ubicacion = "Chalatenango", Estado = "Activa" };
        var c2 = new Comunidad { Nombre = "Comunidad Las Flores", Ubicacion = "La Palma", Estado = "Activa" };
        db.Comunidades.AddRange(c1, c2);
        await db.SaveChangesAsync();

        var s1 = new SistemaSolar { IdComunidad = c1.IdComunidad, Nombre = "Sistema Solar El Pinar",
            FechaInstalacion = DateTime.Today.AddMonths(-6), CapacidadKw = 10m, Estado = "Operativo" };
        var s2 = new SistemaSolar { IdComunidad = c2.IdComunidad, Nombre = "Sistema Solar Las Flores",
            FechaInstalacion = DateTime.Today.AddMonths(-3), CapacidadKw = 7.5m, Estado = "En mantenimiento" };
        db.SistemasSolares.AddRange(s1, s2);
        await db.SaveChangesAsync();

        db.Equipos.AddRange(
            new Equipo { IdSistema = s1.IdSistema, Codigo = "PAN-001", Tipo = "Panel", Marca = "SolarDemo", Modelo = "450W", Estado = "Operativo" },
            new Equipo { IdSistema = s1.IdSistema, Codigo = "BAT-001", Tipo = "Bateria", Marca = "SolarDemo", Modelo = "48V", Estado = "Operativo" },
            new Equipo { IdSistema = s1.IdSistema, Codigo = "INV-001", Tipo = "Inversor", Marca = "SolarDemo", Modelo = "5kW", Estado = "Operativo" },
            new Equipo { IdSistema = s2.IdSistema, Codigo = "PAN-002", Tipo = "Panel", Marca = "SolarDemo", Modelo = "450W", Estado = "Operativo" },
            new Equipo { IdSistema = s2.IdSistema, Codigo = "BAT-002", Tipo = "Bateria", Marca = "SolarDemo", Modelo = "48V", Estado = "En mantenimiento" });
        await db.SaveChangesAsync();

        const string claveDemo = "DemoSolar2026!";
        var admin = new Usuario { Nombre = "Administrador de prueba", Correo = "admin@solargrid.local", Rol = "Administrador", Activo = true };
        var tecnico = new Usuario { Nombre = "Técnico de prueba", Correo = "tecnico@solargrid.local", Rol = "Tecnico", Activo = true };
        var vecino = new Usuario { Nombre = "Vecino de prueba", Correo = "vecino@solargrid.local", Rol = "Comunidad", IdComunidad = c1.IdComunidad, Activo = true };
        foreach (var u in new[] { admin, tecnico, vecino }) u.PasswordHash = hasher.HashPassword(u, claveDemo);
        db.Usuarios.AddRange(admin, tecnico, vecino);
        await db.SaveChangesAsync();

        for (int dias = 10; dias >= 1; dias--)
        {
            db.LecturasEnergia.Add(new LecturaEnergia { IdSistema = s1.IdSistema, IdUsuario = tecnico.IdUsuario,
                FechaHora = DateTime.Today.AddDays(-dias).AddHours(9), GeneracionKwh = 27m + (9-dias)*1.5m, ConsumoKwh = 17m + (9-dias)*0.6m });
            db.LecturasEnergia.Add(new LecturaEnergia { IdSistema = s2.IdSistema, IdUsuario = tecnico.IdUsuario,
                FechaHora = DateTime.Today.AddDays(-dias).AddHours(9), GeneracionKwh = 18m + (9-dias)*1.2m, ConsumoKwh = 12m + (9-dias)*0.5m });
        }
        db.Incidencias.AddRange(
            new Incidencia { IdSistema = s2.IdSistema, IdReporta = tecnico.IdUsuario, Descripcion = "Batería con baja carga al final del día.", Prioridad = "Alta", Estado = "En proceso", FechaCreacion = DateTime.Today.AddDays(-3) },
            new Incidencia { IdSistema = s1.IdSistema, IdReporta = vecino.IdUsuario, Descripcion = "Revisar la conexión de un panel.", Prioridad = "Media", Estado = "Abierta", FechaCreacion = DateTime.Today.AddDays(-1) },
            new Incidencia { IdSistema = s1.IdSistema, IdReporta = tecnico.IdUsuario, Descripcion = "Limpieza preventiva de los paneles.", Prioridad = "Baja", Estado = "Resuelta", Solucion = "Se realizó limpieza general.", FechaCreacion = DateTime.Today.AddDays(-7) });
        await db.SaveChangesAsync();
    }
}
