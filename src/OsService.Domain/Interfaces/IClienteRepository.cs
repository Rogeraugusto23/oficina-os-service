using OsService.Domain.Entities;

namespace OsService.Domain.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(Guid id);
    Task<Cliente?> ObterPorDocumentoAsync(string documento);
    Task<IEnumerable<Cliente>> ListarAsync();
    Task AdicionarAsync(Cliente cliente);
    Task SalvarAlteracoesAsync();
}
