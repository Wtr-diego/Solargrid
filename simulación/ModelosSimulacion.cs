using System.ComponentModel.DataAnnotations;

namespace SolarGrid.Simulacion;

public sealed record ConfiguracionSimulacion : IValidatableObject
{
    [Required] public string Ubicacion { get; init; } = "san-salvador";
    [Required] public string Clima { get; init; } = "Variable";
    [Range(typeof(decimal), "100", "1000", ErrorMessage = "La potencia debe estar entre 100 y 1000 W.")]
    public decimal PotenciaPanelW { get; init; } = 450;
    [Range(1, 24, ErrorMessage = "Selecciona entre 1 y 24 paneles.")]
    public int CantidadPaneles { get; init; } = 1;
    [Range(typeof(decimal), "0.25", "3", ErrorMessage = "El factor de consumo debe estar entre 0.25 y 3.")]
    public decimal FactorConsumo { get; init; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!LugaresSimulacion.Todos.Any(l => l.Id == Ubicacion))
            yield return new ValidationResult("Selecciona una ubicación disponible de El Salvador.", [nameof(Ubicacion)]);
        if (Clima is not ("Soleado" or "Variable" or "Nublado"))
            yield return new ValidationResult("Selecciona un escenario de clima válido.", [nameof(Clima)]);
        if (PotenciaPanelW % 10 != 0)
            yield return new ValidationResult("La potencia debe ser un múltiplo de 10 W.", [nameof(PotenciaPanelW)]);
    }

    public void Validar()
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, new ValidationContext(this), errores, true))
            throw new ArgumentException(string.Join(" ", errores.Select(e => e.ErrorMessage)));
    }
}

// Coordenadas aproximadas de referencia; no son instalaciones reales.
public sealed record LugarSimulacion(string Id, string Nombre, double Latitud, double Longitud, decimal TemperaturaBaseC);

public static class LugaresSimulacion
{
    public static IReadOnlyList<LugarSimulacion> Todos { get; } = Array.AsReadOnly(new[]
    {
        new LugarSimulacion("san-salvador", "San Salvador", 13.6929, -89.2182, 27),
        new LugarSimulacion("santa-ana", "Santa Ana", 13.9942, -89.5597, 26),
        new LugarSimulacion("san-miguel", "San Miguel", 13.4833, -88.1833, 30),
        new LugarSimulacion("chalatenango", "Chalatenango", 14.0333, -88.9333, 28),
        // Referencias aproximadas de las cabeceras; temperaturas ilustrativas del modelo.
        new LugarSimulacion("ahuachapan", "Ahuachapán", 13.9214, -89.8450, 27),
        new LugarSimulacion("sonsonate", "Sonsonate", 13.7189, -89.7242, 29),
        new LugarSimulacion("la-libertad", "La Libertad", 13.6769, -89.2797, 26),
        new LugarSimulacion("cuscatlan", "Cuscatlán", 13.7167, -88.9333, 28),
        new LugarSimulacion("la-paz", "La Paz", 13.5000, -88.8667, 29),
        new LugarSimulacion("cabanas", "Cabañas", 13.8667, -88.6333, 27),
        new LugarSimulacion("san-vicente", "San Vicente", 13.6333, -88.8000, 28),
        new LugarSimulacion("usulutan", "Usulután", 13.3500, -88.4500, 30),
        new LugarSimulacion("morazan", "Morazán", 13.7667, -88.1000, 27),
        new LugarSimulacion("la-union", "La Unión", 13.3333, -87.8500, 30)
    });

    public static LugarSimulacion Obtener(string id) => Todos.FirstOrDefault(l => l.Id == id)
        ?? throw new ArgumentException("Ubicación de simulación no válida.", nameof(id));
}

public sealed record LecturaSimulada
{
    public DateTime Hora { get; init; }
    public decimal IrradianciaWm2 { get; init; }
    public decimal TemperaturaAmbienteC { get; init; }
    public decimal TemperaturaPanelC { get; init; }
    public decimal PotenciaSolarW { get; init; }
    public decimal PotenciaConsumoW { get; init; }
    public decimal GeneracionKwh { get; init; }
    public decimal ConsumoKwh { get; init; }
    public decimal AutoconsumoKwh { get; init; }
    public decimal ImportacionKwh { get; init; }
    public decimal ExcedenteKwh { get; init; }
}

public sealed record EstadoSimulacion
{
    public Guid Id { get; init; }
    public string Estado { get; init; } = "Lista";
    public DateTime Fecha { get; init; }
    public ConfiguracionSimulacion Configuracion { get; init; } = new();
    public LugarSimulacion Lugar => LugaresSimulacion.Obtener(Configuracion.Ubicacion);
    public decimal PotenciaInstaladaW => Configuracion.PotenciaPanelW * Configuracion.CantidadPaneles;
    public int HorasCompletadas => Lecturas.Count;
    public int TotalHoras => 24;
    public int IntervaloSegundos => 2;
    public decimal GeneracionKwh => Lecturas.Sum(l => l.GeneracionKwh);
    public decimal ConsumoKwh => Lecturas.Sum(l => l.ConsumoKwh);
    public decimal AutoconsumoKwh => Lecturas.Sum(l => l.AutoconsumoKwh);
    public decimal ImportacionKwh => Lecturas.Sum(l => l.ImportacionKwh);
    public decimal ExcedenteKwh => Lecturas.Sum(l => l.ExcedenteKwh);
    public decimal CoberturaPorcentaje => ConsumoKwh == 0 ? 0 : Math.Round(AutoconsumoKwh / ConsumoKwh * 100, 1);
    public int? IdSistemaGuardado { get; init; }
    public IReadOnlyList<LecturaSimulada> Lecturas { get; init; } = Array.Empty<LecturaSimulada>();
}
