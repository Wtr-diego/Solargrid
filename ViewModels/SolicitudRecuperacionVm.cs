using System.ComponentModel.DataAnnotations;

namespace SolarGrid.ViewModels;

public sealed class SolicitudRecuperacionVm
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    [StringLength(160)]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu número de teléfono.")]
    [StringLength(24)]
    [RegularExpression(@"^\+?[0-9][0-9 ()-]*$", ErrorMessage = "Escribe un número de teléfono válido.")]
    public string Telefono { get; set; } = "";
}
