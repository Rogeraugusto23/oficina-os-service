using OsService.Application.Common;
using OsService.Domain.Interfaces;
using DomainOrdemServico = OsService.Domain.Entities.OrdemServico;

namespace OsService.Application.UseCases.OrdensServico;

public class ConsultarOrdemServicoUseCase
{
    private readonly IOrdemServicoRepository _repository;

    public ConsultarOrdemServicoUseCase(IOrdemServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<DomainOrdemServico>> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id);

        if (ordem == null)
            return OperationResult<DomainOrdemServico>.Falha(TipoErro.NaoEncontrado, "Ordem de serviço não encontrada.");

        return OperationResult<DomainOrdemServico>.Ok(ordem);
    }
}
