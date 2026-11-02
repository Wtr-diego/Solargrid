using System.ComponentModel.DataAnnotations;

namespace SolarGrid.ViewModels;

public class IngresoVm
{
    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "Correo no válido.")]
    public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)] public string Contrasena { get; set; } = "";
}

public class ComunidadVm
{
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(100)]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "La ubicación es obligatoria."), StringLength(150)]
    public string Ubicacion { get; set; } = "";
    [Required]
    [RegularExpression("^(Activa|Inactiva)$", ErrorMessage = "Selecciona un estado válido.")]
    public string Estado { get; set; } = "Activa";
}

public class SistemaVm
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una comunidad.")]
    public int IdComunidad { get; set; }
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(120)]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Selecciona la fecha de instalación.")]
    public DateTime FechaInstalacion { get; set; } = DateTime.Today;
    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "La capacidad debe ser mayor que cero.")]
    public decimal CapacidadKw { get; set; }
    [RegularExpression("^(Operativo|En mantenimiento|Fuera de servicio)$", ErrorMessage = "Selecciona un estado válido.")]
    public string Estado { get; set; } = "Operativo";
}

public class LecturaVm
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un sistema solar.")]
    public int IdSistema { get; set; }
    [Required]
    public DateTime FechaHora { get; set; } = DateTime.Now;
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "La generación no puede ser negativa.")]
    public decimal GeneracionKwh { get; set; }
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "El consumo no puede ser negativo.")]
    public decimal ConsumoKwh { get; set; }
}


public class EquipoVm
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un sistema.")]
    public int IdSistema { get; set; }
    [Required(ErrorMessage = "Escribe un código."), StringLength(40)]
    public string Codigo { get; set; } = "";
    [Required(ErrorMessage = "Selecciona un tipo."), RegularExpression("^(Panel|Bateria|Inversor|Controlador|Otro)$", ErrorMessage = "Tipo no válido.")]
    public string Tipo { get; set; } = "Panel";
    [StringLength(80)] public string Marca { get; set; } = "";
    [StringLength(80)] public string Modelo { get; set; } = "";
    [RegularExpression("^(Operativo|En mantenimiento|Fuera de servicio)$", ErrorMessage = "Estado no válido.")]
    public string Estado { get; set; } = "Operativo";
}

public class IncidenciaVm
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un sistema.")]
    public int IdSistema { get; set; }
    public int? IdEquipo { get; set; }
    [Required(ErrorMessage = "Describe el problema."), StringLength(1000, MinimumLength=5, ErrorMessage="Describe el problema con al menos 5 caracteres.")]
    public string Descripcion { get; set; } = "";
    [RegularExpression("^(Baja|Media|Alta)$", ErrorMessage = "Prioridad no válida.")]
    public string Prioridad { get; set; } = "Media";
}

public class EstadoIncidenciaVm
{
    [Required, RegularExpression("^(Abierta|En proceso|Resuelta|Cerrada)$", ErrorMessage="Estado no válido.")]
    public string Estado { get; set; } = "Abierta";
    [StringLength(1000)] public string? Solucion { get; set; }
}
