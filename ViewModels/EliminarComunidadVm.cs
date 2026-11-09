namespace SolarGrid.ViewModels;

public class EliminarComunidadVm
{
    public int IdComunidad { get; set; }
    public string Nombre { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public int SistemasAsociados { get; set; }
    public int UsuariosAsociados { get; set; }
    public bool PuedeEliminar => SistemasAsociados == 0 && UsuariosAsociados == 0;
}
