using OsService.Application.Common;
using OsService.Domain.Interfaces;
using DomainCliente = OsService.Domain.Entities.Cliente;

namespace OsService.Application.UseCases.Clientes;

public class ConsultarClienteUseCase
{
    private readonly IClienteRepository _repository;

    public ConsultarClienteUseCase(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<DomainCliente>> ExecutarAsync(Guid id)
    {
        var cliente = await _repository.ObterPorIdAsync(id);
        if (cliente == null)
            return OperationResult<DomainCliente>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");

        return OperationResult<DomainCliente>.Ok(cliente);
    }
}
