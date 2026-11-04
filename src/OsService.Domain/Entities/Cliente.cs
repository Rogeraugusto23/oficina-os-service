using OsService.Domain.Validators;

namespace OsService.Domain.Entities;

public class Cliente
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Documento { get; private set; }

    // Usado pela Function Serverless de autenticação via CPF (Fase 3) para
    // negar acesso a clientes inativos, mesmo com CPF válido e cadastrado.
    public bool Ativo { get; private set; } = true;

    // ✅ Construtor para o EF Core
    private Cliente() { }

    public Cliente(string nome, string documento)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.");

        if (!DocumentoValidator.Validar(documento))
            throw new ArgumentException("CPF ou CNPJ inválido.");

        Id = Guid.NewGuid();
        Nome = nome;
        Documento = new string(documento.Where(char.IsDigit).ToArray());
        Ativo = true;
    }

    public void AtualizarNome(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("Nome é obrigatório.");
        Nome = novoNome;
    }

    public void Inativar() => Ativo = false;

    public void Reativar() => Ativo = true;
}