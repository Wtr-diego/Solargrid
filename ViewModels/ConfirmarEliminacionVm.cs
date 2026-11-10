namespace SolarGrid.ViewModels;

public sealed record ConfirmarEliminacionVm(
    int Id, string Tipo, string Nombre, string? MotivoBloqueo, string? Nota = null,
    string? Confirmacion = null);
