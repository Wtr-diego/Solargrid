using SolarGrid.Simulacion;

// Comprobaciones ejecutables sin paquetes de pruebas adicionales ni acceso a SQL.
var fecha = new DateTime(2026, 11, 7);
var pruebas = new List<(string Nombre, Action Ejecutar)>();

ConfiguracionSimulacion Configurar(string clima = "Soleado", int paneles = 1,
    decimal potencia = 450m, decimal consumo = 1m, string ubicacion = "san-salvador") => new()
{
    Ubicacion = ubicacion,
    Clima = clima,
    PotenciaPanelW = potencia,
    CantidadPaneles = paneles,
    FactorConsumo = consumo
};

LecturaSimulada[] Dia(ConfiguracionSimulacion configuracion) =>
    Enumerable.Range(0, 24).Select(hora => ModeloSolar.Calcular(configuracion, fecha, hora)).ToArray();

void Comprobar(bool condicion, string detalle)
{
    if (!condicion) throw new InvalidOperationException(detalle);
}

void Aproximar(decimal actual, decimal esperado, decimal tolerancia, string detalle) =>
    Comprobar(Math.Abs(actual - esperado) <= tolerancia,
        $"{detalle}: esperado {esperado}, obtenido {actual}, tolerancia {tolerancia}.");

void Rechazar(Action accion, string detalle)
{
    try { accion(); }
    catch (ArgumentException) { return; }
    throw new InvalidOperationException(detalle);
}

void ComprobarBalances(LecturaSimulada lectura)
{
    Comprobar(lectura.GeneracionKwh == lectura.AutoconsumoKwh + lectura.ExcedenteKwh,
        $"No se conserva la generación a las {lectura.Hora:HH:mm}.");
    Comprobar(lectura.ConsumoKwh == lectura.AutoconsumoKwh + lectura.ImportacionKwh,
        $"No se conserva el consumo a las {lectura.Hora:HH:mm}.");
    Comprobar(lectura.AutoconsumoKwh == Math.Min(lectura.GeneracionKwh, lectura.ConsumoKwh),
        "El autoconsumo debe ser la energía solar que satisface la demanda del intervalo.");
    Comprobar(lectura.ImportacionKwh == 0m || lectura.ExcedenteKwh == 0m,
        "No debe haber importación y excedente simultáneos en el modelo sin batería.");
}

pruebas.Add(("Noche: generación e irradiancia cero, demanda abastecida por la red", () =>
{
    foreach (var hora in new[] { 0, 1, 2, 22, 23 })
    {
        var lectura = ModeloSolar.Calcular(Configurar(), fecha, hora);
        Comprobar(lectura.IrradianciaWm2 == 0m && lectura.PotenciaSolarW == 0m &&
            lectura.GeneracionKwh == 0m && lectura.AutoconsumoKwh == 0m && lectura.ExcedenteKwh == 0m,
            $"El panel genera energía de noche a las {hora}:00.");
        Comprobar(lectura.ConsumoKwh > 0m && lectura.ImportacionKwh == lectura.ConsumoKwh,
            "La demanda nocturna debe seguir existiendo y cubrirse mediante importación.");
    }
}));

pruebas.Add(("Valores no negativos y balances conservados en todos los escenarios", () =>
{
    foreach (var ubicacion in new[] { "san-salvador", "santa-ana", "san-miguel", "chalatenango" })
    foreach (var clima in new[] { "Soleado", "Variable", "Nublado" })
    foreach (var lectura in Dia(Configurar(clima, ubicacion: ubicacion)))
    {
        var valores = new[] { lectura.IrradianciaWm2, lectura.PotenciaSolarW, lectura.PotenciaConsumoW,
            lectura.GeneracionKwh, lectura.ConsumoKwh, lectura.AutoconsumoKwh,
            lectura.ImportacionKwh, lectura.ExcedenteKwh };
        Comprobar(valores.All(valor => valor >= 0m), $"Valor negativo en {ubicacion}, {clima}.");
        ComprobarBalances(lectura);
    }
}));

pruebas.Add(("Energía compatible con la precisión de la base de datos", () =>
{
    foreach (var lectura in Dia(Configurar("Variable", paneles: 3, potencia: 470m, consumo: 1.37m)))
    {
        var energias = new[] { lectura.GeneracionKwh, lectura.ConsumoKwh,
            lectura.AutoconsumoKwh, lectura.ImportacionKwh, lectura.ExcedenteKwh };
        Comprobar(energias.All(valor => decimal.Round(valor, 2) == valor),
            "La lectura debe quedar redondeada a dos decimales antes de sumarse y guardarse.");
        Aproximar(lectura.GeneracionKwh, lectura.PotenciaSolarW / 1000m, 0.0051m,
            "Generación de un intervalo de una hora");
        Aproximar(lectura.ConsumoKwh, lectura.PotenciaConsumoW / 1000m, 0.0051m,
            "Consumo de un intervalo de una hora");
    }
}));

pruebas.Add(("El clima cambia la producción, sin alterar la demanda configurada", () =>
{
    var soleado = Dia(Configurar());
    var variable = Dia(Configurar("Variable"));
    var nublado = Dia(Configurar("Nublado"));
    Comprobar(soleado.Sum(l => l.GeneracionKwh) > variable.Sum(l => l.GeneracionKwh) &&
        variable.Sum(l => l.GeneracionKwh) > nublado.Sum(l => l.GeneracionKwh),
        "La producción diaria debe disminuir de Soleado a Variable y Nublado.");
    Comprobar(soleado.Select(l => l.ConsumoKwh).SequenceEqual(nublado.Select(l => l.ConsumoKwh)),
        "El cambio de clima no debe modificar por sí solo el perfil de demanda.");
}));

pruebas.Add(("Duplicar los paneles duplica la potencia solar y conserva el consumo", () =>
{
    var uno = Dia(Configurar());
    var dos = Dia(Configurar(paneles: 2));
    for (var i = 0; i < 24; i++)
    {
        // Cada potencia se presenta con un decimal: se admite su error combinado de redondeo.
        Aproximar(dos[i].PotenciaSolarW, 2m * uno[i].PotenciaSolarW, 0.15m,
            $"Escala de potencia con dos paneles, hora {i}");
        Comprobar(dos[i].ConsumoKwh == uno[i].ConsumoKwh, "La capacidad no cambia la demanda.");
    }
    Aproximar(dos.Sum(l => l.GeneracionKwh), 2m * uno.Sum(l => l.GeneracionKwh), 0.24m,
        "La energía diaria escala con la capacidad, con redondeo por intervalo");
}));

pruebas.Add(("Duplicar la potencia nominal equivale a duplicar el número de paneles", () =>
{
    var dosPaneles = Dia(Configurar(paneles: 2));
    var panelDoble = Dia(Configurar(potencia: 900m));
    for (var i = 0; i < 24; i++)
    {
        Comprobar(dosPaneles[i].PotenciaSolarW == panelDoble[i].PotenciaSolarW &&
            dosPaneles[i].GeneracionKwh == panelDoble[i].GeneracionKwh,
            $"Configuraciones de igual capacidad difieren a las {i}:00.");
    }
}));

pruebas.Add(("El factor de consumo escala la demanda sin modificar la generación", () =>
{
    var normal = Dia(Configurar());
    var doble = Dia(Configurar(consumo: 2m));
    for (var i = 0; i < 24; i++)
    {
        Aproximar(doble[i].PotenciaConsumoW, 2m * normal[i].PotenciaConsumoW, 0.15m,
            $"Escala del consumo, hora {i}");
        Comprobar(doble[i].GeneracionKwh == normal[i].GeneracionKwh,
            "La carga eléctrica no debe modificar la generación del panel.");
        Comprobar(doble[i].ImportacionKwh >= normal[i].ImportacionKwh,
            "Aumentar demanda no debe reducir la importación.");
    }
}));

pruebas.Add(("Las mismas entradas producen las mismas lecturas", () =>
{
    var configuracion = Configurar("Variable", ubicacion: "chalatenango");
    var primera = Dia(configuracion);
    var segunda = Dia(configuracion);
    for (var i = 0; i < 24; i++)
    {
        var a = primera[i];
        var b = segunda[i];
        Comprobar(a.Hora == b.Hora && a.IrradianciaWm2 == b.IrradianciaWm2 &&
            a.TemperaturaAmbienteC == b.TemperaturaAmbienteC && a.TemperaturaPanelC == b.TemperaturaPanelC &&
            a.PotenciaSolarW == b.PotenciaSolarW && a.PotenciaConsumoW == b.PotenciaConsumoW &&
            a.GeneracionKwh == b.GeneracionKwh && a.ConsumoKwh == b.ConsumoKwh &&
            a.AutoconsumoKwh == b.AutoconsumoKwh && a.ImportacionKwh == b.ImportacionKwh &&
            a.ExcedenteKwh == b.ExcedenteKwh, "La simulación no es reproducible.");
    }
}));

pruebas.Add(("Las 24 lecturas cubren un único día sin duplicar horas", () =>
{
    var lecturas = Dia(Configurar());
    Comprobar(lecturas.Length == 24 && lecturas.Select(l => l.Hora).Distinct().Count() == 24,
        "Deben existir 24 marcas de tiempo distintas.");
    for (var i = 0; i < 24; i++)
        Comprobar(lecturas[i].Hora == fecha.AddHours(i),
            "Cada lectura debe estar fechada al inicio de su intervalo, dentro del día elegido.");
}));

pruebas.Add(("Configuraciones mínima y máxima producen balances válidos", () =>
{
    foreach (var configuracion in new[]
    {
        Configurar("Nublado", paneles: 1, potencia: 100m, consumo: 3m),
        Configurar("Soleado", paneles: 24, potencia: 1000m, consumo: 0.25m)
    })
    {
        var lecturas = Dia(configuracion);
        Comprobar(lecturas.Sum(l => l.GeneracionKwh) > 0m, "La generación diaria debe ser positiva.");
        foreach (var lectura in lecturas)
        {
            Comprobar(lectura.PotenciaSolarW >= 0m &&
                lectura.PotenciaSolarW <= configuracion.PotenciaPanelW * configuracion.CantidadPaneles,
                "La potencia producida queda fuera de la capacidad nominal del escenario.");
            ComprobarBalances(lectura);
        }
    }
}));

pruebas.Add(("Rechaza horas fuera del día", () =>
{
    Rechazar(() => ModeloSolar.Calcular(Configurar(), fecha, -1), "Aceptó una hora negativa.");
    Rechazar(() => ModeloSolar.Calcular(Configurar(), fecha, 24), "Aceptó la hora 24 de otro día.");
}));

pruebas.Add(("Rechaza ubicaciones y climas desconocidos", () =>
{
    Rechazar(() => ModeloSolar.Calcular(Configurar(ubicacion: "fuera-del-catalogo"), fecha, 12),
        "Aceptó una ubicación inexistente.");
    Rechazar(() => ModeloSolar.Calcular(Configurar(clima: "inexistente"), fecha, 12),
        "Aceptó un clima inexistente.");
}));

var fallidas = 0;
foreach (var prueba in pruebas)
{
    try
    {
        prueba.Ejecutar();
        Console.WriteLine($"APROBADA | {prueba.Nombre}");
    }
    catch (Exception error)
    {
        fallidas++;
        Console.Error.WriteLine($"FALLIDA  | {prueba.Nombre}\n           {error.Message}");
    }
}
Console.WriteLine($"\n{pruebas.Count - fallidas}/{pruebas.Count} pruebas aprobadas; {fallidas} fallidas.");
return fallidas == 0 ? 0 : 1;
