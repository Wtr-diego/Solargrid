using System.ComponentModel.DataAnnotations;

namespace SolarGrid.Models;

public class Comunidad
{
    public int IdComunidad { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, StringLength(150)] public string Ubicacion { get; set; } = "";
    [Required, StringLength(20)] public string Estado { get; set; } = "Activa";
    public ICollection<SistemaSolar> Sistemas { get; set; } = new List<SistemaSolar>();
}

public class Usuario
{
    public int IdUsuario { get; set; }
    public int? IdComunidad { get; set; }
    public Comunidad? Comunidad { get; set; }
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    [Required, EmailAddress, StringLength(160)] public string Correo { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    [Required, StringLength(30)] public string Rol { get; set; } = "Comunidad";
    public bool Activo { get; set; } = true;
}

public class SistemaSolar
{
    public int IdSistema { get; set; }
    public int IdComunidad { get; set; }
    public Comunidad Comunidad { get; set; } = null!;
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    public DateTime FechaInstalacion { get; set; }
    [Range(typeof(decimal), "0.01", "999999.99")]
    public decimal CapacidadKw { get; set; }
    [Required, StringLength(30)] public string Estado { get; set; } = "Operativo";
}

public class Equipo
{
    public int IdEquipo { get; set; }
    public int IdSistema { get; set; }
    public SistemaSolar Sistema { get; set; } = null!;
    [Required, StringLength(40)] public string Codigo { get; set; } = "";
    [Required, StringLength(40)] public string Tipo { get; set; } = "";
    [StringLength(80)] public string Marca { get; set; } = "";
    [StringLength(80)] public string Modelo { get; set; } = "";
    [Required, StringLength(30)] public string Estado { get; set; } = "Operativo";
    // La ubicación es opcional: los equipos antiguos siguen funcionando sin coordenadas.
    public UbicacionPanel? UbicacionPanel { get; set; }
}

// La ubicación se guarda por panel, sin modificar las tablas existentes.
public class UbicacionPanel
{
    public int IdEquipo { get; set; }
    public Equipo Equipo { get; set; } = null!;
    [Range(-90, 90)] public double Latitud { get; set; }
    [Range(-180, 180)] public double Longitud { get; set; }
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}

public class LecturaEnergia
{
    public int IdLectura { get; set; }
    public int IdSistema { get; set; }
    public SistemaSolar Sistema { get; set; } = null!;
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime FechaHora { get; set; }
    [Range(typeof(decimal), "0", "999999999")]
    public decimal GeneracionKwh { get; set; }
    [Range(typeof(decimal), "0", "999999999")]
    public decimal ConsumoKwh { get; set; }
}

public class Incidencia
{
    public int IdIncidencia { get; set; }
    public int IdSistema { get; set; }
    public SistemaSolar Sistema { get; set; } = null!;
    public int? IdEquipo { get; set; }
    public Equipo? Equipo { get; set; }
    public int IdReporta { get; set; }
    public Usuario Reporta { get; set; } = null!;
    [Required, StringLength(1000)] public string Descripcion { get; set; } = "";
    [Required, StringLength(20)] public string Prioridad { get; set; } = "Media";
    [Required, StringLength(20)] public string Estado { get; set; } = "Abierta";
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    [StringLength(1000)] public string? Solucion { get; set; }
}

public class Reporte
{
    public int IdReporte { get; set; }
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int IdSistema { get; set; }
    public SistemaSolar Sistema { get; set; } = null!;
    [Required, StringLength(50)] public string Tipo { get; set; } = "";
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFin { get; set; }
}

public class Historial
{
    public int IdHistorial { get; set; }
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;
    [Required, StringLength(60)] public string Tabla { get; set; } = "";
    public int IdRegistro { get; set; }
    [Required, StringLength(20)] public string Accion { get; set; } = "";
    public DateTime FechaHora { get; set; } = DateTime.Now;
    [StringLength(500)] public string Detalle { get; set; } = "";
}
