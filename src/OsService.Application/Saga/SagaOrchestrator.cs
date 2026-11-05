using Microsoft.Extensions.Logging;
using OsService.Application.Common;
using OsService.Application.UseCases.OrdensServico;
using OsService.Domain.Enums;
using OsService.Domain.Events;
using OsService.Domain.Interfaces;

namespace OsService.Application.Saga;

/// <summary>
/// Orquestrador da Saga (abordagem orquestrada: o OS Service decide o próximo
/// passo ou a compensação a cada evento recebido dos outros serviços).
///
///   OS aberta → orçamento enviado → [Billing cria cobrança]
///   PagamentoAprovado   → aprova orçamento → OrcamentoAprovadoEvent → [Execução]
///   PagamentoRejeitado  → compensação: rejeita orçamento/cancela OS → [Billing cancela]
///   ExecucaoFinalizada(sucesso)  → OS Finalizada
///   ExecucaoFinalizada(falha)    → compensação: cancela OS + OrdemServicoCanceladaEvent
///                                  → [Billing estorna/cancela cobrança]
///
/// Todos os handlers são idempotentes (SQS entrega "pelo menos uma vez"):
/// se a OS não está no estado esperado, o evento é ignorado e logado.
/// </summary>
public class SagaOrchestrator
{
    private readonly IOrdemServicoRepository _repository;
    private readonly AprovarOrcamentoUseCase _aprovar;
    private readonly RejeitarOrcamentoUseCase _rejeitar;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<SagaOrchestrator> _logger;

    public SagaOrchestrator(
        IOrdemServicoRepository repository,
        AprovarOrcamentoUseCase aprovar,
        RejeitarOrcamentoUseCase rejeitar,
        IEventPublisher publisher,
        ILogger<SagaOrchestrator> logger)
    {
        _repository = repository;
        _aprovar = aprovar;
        _rejeitar = rejeitar;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task OnPagamentoAprovadoAsync(Guid ordemServicoId)
    {
        var ordem = await _repository.ObterPorIdAsync(ordemServicoId);
        if (ordem == null || ordem.Status != StatusOrdemServico.AguardandoAprovacao)
        {
            _logger.LogWarning("Saga {OrdemServicoId}: PagamentoAprovado ignorado (OS ausente ou status {Status})",
                ordemServicoId, ordem?.Status);
            return;
        }

        await _aprovar.ExecutarAsync(ordemServicoId);
        _logger.LogInformation("Saga {OrdemServicoId}: pagamento aprovado → orçamento aprovado, execução solicitada", ordemServicoId);
    }

    public async Task OnPagamentoRejeitadoAsync(Guid ordemServicoId, string? motivo)
    {
        var ordem = await _repository.ObterPorIdAsync(ordemServicoId);
        if (ordem == null || ordem.Status != StatusOrdemServico.AguardandoAprovacao)
        {
            _logger.LogWarning("Saga {OrdemServicoId}: PagamentoRejeitado ignorado (OS ausente ou status {Status})",
                ordemServicoId, ordem?.Status);
            return;
        }

        await _rejeitar.ExecutarAsync(ordemServicoId);
        _logger.LogWarning("Saga {OrdemServicoId}: pagamento rejeitado ({Motivo}) → COMPENSAÇÃO: OS cancelada",
            ordemServicoId, motivo);
    }

    public async Task OnExecucaoFinalizadaAsync(Guid ordemServicoId, bool sucesso, string? motivoFalha)
    {
        var ordem = await _repository.ObterPorIdAsync(ordemServicoId);
        if (ordem == null || ordem.Status != StatusOrdemServico.EmExecucao)
        {
            _logger.LogWarning("Saga {OrdemServicoId}: ExecucaoFinalizada ignorada (OS ausente ou status {Status})",
                ordemServicoId, ordem?.Status);
            return;
        }

        if (sucesso)
        {
            ordem.AvancarStatus(); // EmExecucao → Finalizada
            await _repository.SalvarAlteracoesAsync();
            _logger.LogInformation("Saga {OrdemServicoId}: execução concluída → OS finalizada. Saga encerrada com sucesso.", ordemServicoId);
            return;
        }

        ordem.CancelarOS();
        await _repository.SalvarAlteracoesAsync();

        await _publisher.PublicarAsync(new OrdemServicoCanceladaEvent
        {
            CorrelationId = ordem.Id,
            OrdemServicoId = ordem.Id,
            NumeroOS = ordem.NumeroOS,
            Motivo = motivoFalha ?? "Falha na execução"
        });
        _logger.LogWarning("Saga {OrdemServicoId}: execução falhou ({Motivo}) → COMPENSAÇÃO: OS cancelada, estorno solicitado ao Billing",
            ordemServicoId, motivoFalha);
    }
}
