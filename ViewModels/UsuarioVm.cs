using System.ComponentModel.DataAnnotations;

namespace SolarGrid.ViewModels;

public class UsuarioVm
{
    [Required(ErrorMessage = "Escribe el nombre.")]
    [StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Escribe el correo.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(160, ErrorMessage = "El correo admite hasta 160 caracteres.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Selecciona un rol.")]
    [RegularExpression(@"^(Administrador|Tecnico|Comunidad)\z", ErrorMessage = "Selecciona un rol válido.")]
    public string Rol { get; set; } = "";

    public int? IdComunidad { get; set; }

    [DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 128 caracteres.")]
    public string? Contrasena { get; set; }

    public bool? Activo { get; set; }
}
