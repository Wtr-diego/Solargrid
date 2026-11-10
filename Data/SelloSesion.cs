using System.Security.Cryptography;
using System.Text;

namespace SolarGrid.Data;

public static class SelloSesion
{
    public const string Claim = "solargrid:sello-sesion";

    public static string Calcular(string passwordHash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)));
}
