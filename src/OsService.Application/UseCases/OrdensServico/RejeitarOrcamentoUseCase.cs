using OsService.Application.Common;
using OsService.Application.DTOs.OrdensServico;
using OsService.Domain.Enums;
using OsService.Domain.Events;
using OsService.Domain.Interfaces;

namespace OsService.Application.UseCases.OrdensServico;

/// <summary>
/// Fim antecipado da Saga: rejeição do orçamento cancela a OS e publica
/// OrcamentoRejeitadoEvent, permitindo que o Billing Service encerre/estorne
/// qualquer cobrança pendente associada (ação de compensação do lado dele).
/// </summary>
public class RejeitarOrcamentoUseCase
{
    private readonly IOrdemServicoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public RejeitarOrcamentoUseCase(IOrdemServicoRepository repository, IEventPublisher eventPublisher)
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

        ordem.CancelarOS();
        await _repository.SalvarAlteracoesAsync();

        await _eventPublisher.PublicarAsync(new OrcamentoRejeitadoEvent
        {
            CorrelationId = ordem.Id,
            OrdemServicoId = ordem.Id,
            NumeroOS = ordem.NumeroOS
        });

        return OperationResult<StatusAtualizadoOutput>.Ok(new StatusAtualizadoOutput
        {
            Mensagem = "Orçamento rejeitado pelo cliente. OS cancelada.",
            NumeroOS = ordem.NumeroOS,
            Status = ordem.Status.ToString()
        });
    }
}
