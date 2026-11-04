using System.Text.RegularExpressions;

namespace OsService.Domain.ValueObjects;

public record Placa
{
    public string Valor { get; init; }

    private static readonly Regex _regex = new(
        @"^[A-Z]{3}[0-9]{4}$|^[A-Z]{3}[0-9]{1}[A-Z]{1}[0-9]{2}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public Placa(string valor)
    {
        valor = valor.Replace("-", "").Trim().ToUpper();
        if (!Validar(valor))
            throw new ArgumentException("Placa inválida. Use ABC1234 ou ABC1D23.");
        Valor = valor;
    }

    public static bool Validar(string placa)
    {
        if (string.IsNullOrWhiteSpace(placa)) return false;
        return _regex.IsMatch(placa.Replace("-", "").Trim());
    }

    public override string ToString() => $"{Valor[..3]}-{Valor[3..]}";
}