using OsService.Application.Common;
using OsService.Domain.Interfaces;
using DomainCliente = OsService.Domain.Entities.Cliente;

namespace OsService.Application.UseCases.Clientes;

public class ListarClientesUseCase
{
    private readonly IClienteRepository _repository;

    public ListarClientesUseCase(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<IEnumerable<DomainCliente>>> ExecutarAsync()
    {
        var clientes = await _repository.ListarAsync();
        return OperationResult<IEnumerable<DomainCliente>>.Ok(clientes);
    }
}
