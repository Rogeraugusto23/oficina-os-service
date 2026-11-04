using OsService.Application.Common;
using OsService.Application.DTOs.Clientes;
using OsService.Domain.Interfaces;
using DomainCliente = OsService.Domain.Entities.Cliente;

namespace OsService.Application.UseCases.Clientes;

public class CriarClienteUseCase
{
    private readonly IClienteRepository _repository;

    public CriarClienteUseCase(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<DomainCliente>> ExecutarAsync(CriarClienteInput input)
    {
        var existente = await _repository.ObterPorDocumentoAsync(input.Documento);
        if (existente != null)
            return OperationResult<DomainCliente>.Falha(
                TipoErro.OperacaoInvalida, "Já existe um cliente cadastrado com este documento.");

        DomainCliente cliente;
        try
        {
            cliente = new DomainCliente(input.Nome, input.Documento);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<DomainCliente>.Falha(TipoErro.ValidacaoFalhou, ex.Message);
        }

        await _repository.AdicionarAsync(cliente);
        await _repository.SalvarAlteracoesAsync();

        return OperationResult<DomainCliente>.Ok(cliente, "Cliente cadastrado com sucesso.");
    }
}
