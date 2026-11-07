namespace SolarGrid.ViewModels;

public class AlertaConsumoVm
{
    public string Comunidad { get; set; } = "";
    public string Sistema { get; set; } = "";
    public decimal GeneracionKwh { get; set; }
    public decimal ConsumoKwh { get; set; }
    public decimal DiferenciaKwh => ConsumoKwh - GeneracionKwh;
}