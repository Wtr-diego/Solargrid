namespace SolarGrid.Simulacion;

/// <summary>Modelo didáctico determinista: un intervalo de una hora, evaluado en su punto medio.</summary>
public static class ModeloSolar
{
    public static LecturaSimulada Calcular(ConfiguracionSimulacion configuracion, DateTime fecha, int hora)
    {
        configuracion.Validar();
        if (hora is < 0 or > 23) throw new ArgumentOutOfRangeException(nameof(hora));
        var lugar = LugaresSimulacion.Obtener(configuracion.Ubicacion);
        var puntoMedio = hora + 0.5;
        // Día idealizado de 06:00 a 18:00. No consulta meteorología ni calcula efemérides.
        var altura = Math.Max(0, Math.Sin(Math.PI * (puntoMedio - 6) / 12));
        var nubosidad = configuracion.Clima switch
        {
            "Soleado" => 0.95,
            "Nublado" => 0.28 + 0.06 * Math.Sin(puntoMedio * 1.7),
            _ => 0.62 + 0.20 * Math.Sin(puntoMedio * 1.7)
        };
        var irradiancia = 1000m * (decimal)(altura * nubosidad);
        var ambiente = lugar.TemperaturaBaseC + (decimal)(5 * Math.Sin(Math.PI * (puntoMedio - 8) / 12));
        var temperaturaPanel = ambiente + irradiancia * 0.03m;
        var factorTemperatura = Math.Max(0, 1 - 0.004m * (temperaturaPanel - 25));
        var instalada = configuracion.PotenciaPanelW * configuracion.CantidadPaneles;
        var potenciaSolar = Math.Clamp(instalada * irradiancia / 1000 * factorTemperatura * 0.86m, 0, instalada);
        var demandaBase = hora switch
        {
            < 6 => 80m,
            < 9 => 350m,
            < 17 => 160m,
            < 22 => 420m,
            _ => 120m
        };
        var potenciaConsumo = demandaBase * configuracion.FactorConsumo * (1 + (decimal)Math.Sin(puntoMedio * 2.1) * 0.08m);
        // Cada fila usa la misma precisión que LecturasEnergia (decimal(14,2)).
        // Así el total mostrado coincide exactamente con el guardado en SQL Server.
        var generacion = Math.Round(potenciaSolar / 1000, 2, MidpointRounding.AwayFromZero);
        var consumo = Math.Round(potenciaConsumo / 1000, 2, MidpointRounding.AwayFromZero);
        var autoconsumo = Math.Min(generacion, consumo);
        return new LecturaSimulada
        {
            Hora = DateTime.SpecifyKind(fecha.Date.AddHours(hora), DateTimeKind.Unspecified),
            IrradianciaWm2 = Math.Round(irradiancia, 1),
            TemperaturaAmbienteC = Math.Round(ambiente, 1),
            TemperaturaPanelC = Math.Round(temperaturaPanel, 1),
            PotenciaSolarW = Math.Round(potenciaSolar, 1),
            PotenciaConsumoW = Math.Round(potenciaConsumo, 1),
            GeneracionKwh = generacion,
            ConsumoKwh = consumo,
            AutoconsumoKwh = autoconsumo,
            ImportacionKwh = consumo - autoconsumo,
            ExcedenteKwh = generacion - autoconsumo
        };
    }
}
