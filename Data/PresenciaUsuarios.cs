using Microsoft.Extensions.Caching.Memory;

namespace SolarGrid.Data;

public sealed class PresenciaUsuarios(IMemoryCache cache)
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(2);

    public void Registrar(int idUsuario) =>
        cache.Set((typeof(PresenciaUsuarios), idUsuario), true, Vigencia);

    public bool EnLinea(int idUsuario) =>
        cache.TryGetValue((typeof(PresenciaUsuarios), idUsuario), out _);
}
