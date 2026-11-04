using OsService.Application.Common;
using OsService.Domain.Enums;
using OsService.Domain.Interfaces;
using DomainOrdemServico = OsService.Domain.Entities.OrdemServico;

namespace OsService.Application.UseCases.OrdensServico;

/// <summary>
/// Regra de negócio (Fase 2):
/// - Não lista OS Finalizada/Entregue (exclusão lógica, não física — continuam no banco)
/// - Ordena por prioridade de status: EmExecucao > AguardandoAprovacao > EmDiagnostico > Recebida
/// - Dentro do mesmo status, mais antigas primeiro
/// </summary>
public class ListarOrdensServicoUseCase
{
    private static readonly IReadOnlyDictionary<StatusOrdemServico, int> Prioridade = new Dictionary<StatusOrdemServico, int>
    {
        { StatusOrdemServico.EmExecucao, 1 },
        { StatusOrdemServico.AguardandoAprovacao, 2 },
        { StatusOrdemServico.EmDiagnostico, 3 },
        { StatusOrdemServico.Recebida, 4 },
        { StatusOrdemServico.Cancelada, 5 }
    };

    private static readonly StatusOrdemServico[] StatusExcluidos =
    {
        StatusOrdemServico.Finalizada,
        StatusOrdemServico.Entregue
    };

    private readonly IOrdemServicoRepository _repository;

    public ListarOrdensServicoUseCase(IOrdemServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<IEnumerable<DomainOrdemServico>>> ExecutarAsync()
    {
        var todas = await _repository.ListarAsync();

        var ordenadas = todas
            .Where(o => !StatusExcluidos.Contains(o.Status))
            .OrderBy(o => Prioridade.GetValueOrDefault(o.Status, 99))
            .ThenBy(o => o.DataAbertura)
            .ToList();

        return OperationResult<IEnumerable<DomainOrdemServico>>.Ok(ordenadas);
    }
}
