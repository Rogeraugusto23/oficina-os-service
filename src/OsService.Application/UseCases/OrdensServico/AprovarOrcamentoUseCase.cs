using OsService.Application.Common;
using OsService.Application.DTOs.OrdensServico;
using OsService.Domain.Enums;
using OsService.Domain.Events;
using OsService.Domain.Interfaces;

namespace OsService.Application.UseCases.OrdensServico;

/// <summary>
/// Passo 2 da Saga orquestrada: aprovação do orçamento (feita pelo cliente,
/// hoje acionada via API) publica OrcamentoAprovadoEvent, que o Execução Service
/// escuta para iniciar a fila de execução. Se a execução falhar depois, o
/// Execução Service publica ExecucaoFinalizadaEvent(Sucesso=false) e o OS Service
/// executa a compensação (ver AtualizarStatusPorEmailUseCase / futura etapa de
/// consumo de eventos, Dias 10-11 do roadmap).
/// </summary>
public class AprovarOrcamentoUseCase
{
    private readonly IOrdemServicoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public AprovarOrcamentoUseCase(IOrdemServicoRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<OperationResult<StatusAtualizadoOutput>> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id);
        if (ordem == null)
            return OperationResult<StatusAtualizadoOutput>.Falha(TipoErro.NaoEncontrado, "Ordem de serviço não encontrada.");

        if (ordem.Status != StatusOrdemServico.AguardandoAprovacao)
            return OperationResult<StatusAtualizadoOutput>.Falha(
                TipoErro.OperacaoInvalida,
                $"A OS precisa estar Aguardando Aprovação. Status atual: {ordem.Status}.");

        ordem.AvancarStatus(); // AguardandoAprovacao → EmExecucao
        await _repository.SalvarAlteracoesAsync();

        await _eventPublisher.PublicarAsync(new OrcamentoAprovadoEvent
        {
            CorrelationId = ordem.Id,
            OrdemServicoId = ordem.Id,
            NumeroOS = ordem.NumeroOS,
            ValorTotal = ordem.ValorTotal
        });

        return OperationResult<StatusAtualizadoOutput>.Ok(new StatusAtualizadoOutput
        {
            Mensagem = "Orçamento aprovado! OS iniciada.",
            NumeroOS = ordem.NumeroOS,
            Status = ordem.Status.ToString()
        });
    }
}
