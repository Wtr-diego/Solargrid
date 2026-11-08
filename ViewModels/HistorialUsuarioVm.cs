namespace SolarGrid.ViewModels;

public record HistorialUsuarioVm(
    int IdHistorial,
    DateTime FechaHora,
    int IdAdministrador,
    string Administrador,
    int IdUsuario,
    string? Usuario,
    string Accion,
    string Detalle);
