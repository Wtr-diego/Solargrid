using Microsoft.EntityFrameworkCore;
using SolarGrid.Models;

namespace SolarGrid.Data;

public class SolarGridDbContext(DbContextOptions<SolarGridDbContext> options) : DbContext(options)
{
    public DbSet<Comunidad> Comunidades => Set<Comunidad>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<SistemaSolar> SistemasSolares => Set<SistemaSolar>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<UbicacionPanel> UbicacionesPanel => Set<UbicacionPanel>();
    public DbSet<LecturaEnergia> LecturasEnergia => Set<LecturaEnergia>();
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();
    public DbSet<Reporte> Reportes => Set<Reporte>();
    public DbSet<Historial> Historial => Set<Historial>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Comunidad>(e =>
        {
            e.ToTable("Comunidades", t => t.HasCheckConstraint("CK_Comunidades_Estado", "[Estado] IN ('Activa','Inactiva')"));
            e.HasKey(x => x.IdComunidad);
            e.HasIndex(x => x.Nombre).IsUnique();
        });
        b.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios", t => t.HasCheckConstraint("CK_Usuarios_Rol", "[Rol] IN ('Administrador','Tecnico','Comunidad')"));
            e.HasKey(x => x.IdUsuario);
            e.HasIndex(x => x.Correo).IsUnique();
            e.HasOne(x => x.Comunidad).WithMany().HasForeignKey(x => x.IdComunidad).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<SistemaSolar>(e =>
        {
            e.ToTable("SistemasSolares", t => t.HasCheckConstraint("CK_Sistemas_Capacidad", "[CapacidadKw] > 0"));
            e.HasKey(x => x.IdSistema);
            e.Property(x => x.CapacidadKw).HasPrecision(10, 2);
            e.HasOne(x => x.Comunidad).WithMany(c => c.Sistemas).HasForeignKey(x => x.IdComunidad).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Equipo>(e =>
        {
            e.ToTable("Equipos");
            e.HasKey(x => x.IdEquipo);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasOne(x => x.Sistema).WithMany().HasForeignKey(x => x.IdSistema).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<UbicacionPanel>(e =>
        {
            e.ToTable("UbicacionesPanel", t =>
            {
                t.HasCheckConstraint("CK_Ubicacion_Latitud", "[Latitud] BETWEEN -90 AND 90");
                t.HasCheckConstraint("CK_Ubicacion_Longitud", "[Longitud] BETWEEN -180 AND 180");
            });
            e.HasKey(x => x.IdEquipo);
            e.HasOne(x => x.Equipo).WithOne(x => x.UbicacionPanel)
                .HasForeignKey<UbicacionPanel>(x => x.IdEquipo)
                .OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<LecturaEnergia>(e =>
        {
            e.ToTable("LecturasEnergia", t =>
            {
                t.HasCheckConstraint("CK_Lecturas_Generacion", "[GeneracionKwh] >= 0");
                t.HasCheckConstraint("CK_Lecturas_Consumo", "[ConsumoKwh] >= 0");
            });
            e.HasKey(x => x.IdLectura);
            e.Property(x => x.GeneracionKwh).HasPrecision(14, 2);
            e.Property(x => x.ConsumoKwh).HasPrecision(14, 2);
            e.HasOne(x => x.Sistema).WithMany().HasForeignKey(x => x.IdSistema).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Incidencia>(e =>
        {
            e.ToTable("Incidencias", t => t.HasCheckConstraint("CK_Incidencias_Estado", "[Estado] IN ('Abierta','En proceso','Resuelta','Cerrada')"));
            e.HasKey(x => x.IdIncidencia);
            e.HasOne(x => x.Sistema).WithMany().HasForeignKey(x => x.IdSistema).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Equipo).WithMany().HasForeignKey(x => x.IdEquipo).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Reporta).WithMany().HasForeignKey(x => x.IdReporta).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Reporte>(e =>
        {
            e.ToTable("Reportes", t => t.HasCheckConstraint("CK_Reportes_Periodo", "[PeriodoInicio] <= [PeriodoFin]"));
            e.HasKey(x => x.IdReporte);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Sistema).WithMany().HasForeignKey(x => x.IdSistema).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Historial>(e =>
        {
            e.ToTable("Historial");
            e.HasKey(x => x.IdHistorial);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
