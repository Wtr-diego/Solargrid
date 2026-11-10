using SolarGrid.Models;

namespace SolarGrid.ViewModels;

public class MapaComunidadesVm
{
    public List<Comunidad> Comunidades { get; set; } = new();
    public List<PanelEnMapaVm> Paneles { get; set; } = new();
}

public class PanelEnMapaVm
{
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";

    public string NombreVisible
    {
        get
        {
            var nombre = $"{Marca?.Trim()} {Modelo?.Trim()}".Trim();
            return nombre.Length > 0 ? nombre : $"Panel {Codigo}";
        }
    }

    public int IdEquipo { get; set; }
    public string Codigo { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Sistema { get; set; } = "";
    public string Comunidad { get; set; } = "";
    public int IdComunidad { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}
