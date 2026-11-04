using OsService.Domain.Entities;

namespace OsService.Domain.Interfaces;

/// <summary>
/// Contrato de acesso a dados de OrdemServico. Vive no Domain para que a camada
/// de Application dependa apenas desta abstração — nunca do EF Core diretamente.
/// A implementação concreta fica em Infrastructure/Repositories.
/// </summary>
public interface IOrdemServicoRepository
{
    Task<OrdemServico?> ObterPorIdAsync(Guid id);
    Task<OrdemServico?> ObterPorNumeroOSAsync(string numeroOS);
    Task<IEnumerable<OrdemServico>> ListarAsync();
    Task AdicionarAsync(OrdemServico ordemServico);
    Task SalvarAlteracoesAsync();
}
