using OsService.Domain.ValueObjects;

namespace OsService.Domain.Entities;

public class Veiculo
{
    public Guid Id { get; private set; }
    public string Placa { get; private set; }
    public string Marca { get; private set; }
    public string Modelo { get; private set; }
    public int Ano { get; private set; }

    public Guid ClienteId { get; private set; }

    private Veiculo() { }

    public Veiculo(string placa, string marca, string modelo, int ano, Guid clienteId)
    {
        if (!OsService.Domain.ValueObjects.Placa.Validar(placa))
            throw new ArgumentException("Placa inválida. Use ABC1234 ou ABC1D23.");

        if (string.IsNullOrWhiteSpace(marca))
            throw new ArgumentException("Marca é obrigatória.");

        if (string.IsNullOrWhiteSpace(modelo))
            throw new ArgumentException("Modelo é obrigatório.");

        if (ano < 1900 || ano > DateTime.Now.Year + 1)
            throw new ArgumentException("Ano inválido.");  // ✅ linha estava cortada

        Id = Guid.NewGuid();
        Placa = placa.Replace("-", "").Trim().ToUpper();
        Marca = marca;
        Modelo = modelo;
        Ano = ano;
        ClienteId = clienteId;
    }

    public void Atualizar(string marca, string modelo, int ano)
    {
        if (string.IsNullOrWhiteSpace(marca))
            throw new ArgumentException("Marca é obrigatória.");

        if (string.IsNullOrWhiteSpace(modelo))
            throw new ArgumentException("Modelo é obrigatório.");

        if (ano < 1900 || ano > DateTime.Now.Year + 1)
            throw new ArgumentException("Ano inválido.");

        Marca = marca;
        Modelo = modelo;
        Ano = ano;
    }
}