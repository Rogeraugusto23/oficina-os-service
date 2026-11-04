using OsService.Application.Common;
using OsService.Domain.Interfaces;
using DomainCliente = OsService.Domain.Entities.Cliente;

namespace OsService.Application.UseCases.Clientes;

public class InativarClienteUseCase
{
    private readonly IClienteRepository _repository;

    public InativarClienteUseCase(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<DomainCliente>> ExecutarAsync(Guid id)
    {
        var cliente = await _repository.ObterPorIdAsync(id);
        if (cliente == null)
            return OperationResult<DomainCliente>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");

        cliente.Inativar();
        await _repository.SalvarAlteracoesAsync();

        return OperationResult<DomainCliente>.Ok(cliente, "Cliente inativado.");
    }
}
