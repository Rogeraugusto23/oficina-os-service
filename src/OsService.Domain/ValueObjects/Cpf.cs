using OsService.Domain.Validators;

namespace OsService.Domain.ValueObjects;

public record Cpf
{
    public string Numero { get; init; }

    public Cpf(string numero)
    {
        if (!Validar(numero))
            throw new ArgumentException("CPF inválido.");
        Numero = new string(numero.Where(char.IsDigit).ToArray());
    }

    // ✅ Delega para o DocumentoValidator — sem duplicar lógica
    public static bool Validar(string cpf) => DocumentoValidator.ValidarCpf(cpf);

    public override string ToString() =>
        $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..]}";
}