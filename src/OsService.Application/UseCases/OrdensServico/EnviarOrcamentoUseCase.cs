using OsService.Application.Common;
using OsService.Application.DTOs.OrdensServico;
using OsService.Domain.Enums;
using OsService.Domain.Events;
using OsService.Domain.Interfaces;

namespace OsService.Application.UseCases.OrdensServico;

public class EnviarOrcamentoUseCase
{
    private readonly IOrdemServicoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public EnviarOrcamentoUseCase(IOrdemServicoRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<OperationResult<OrcamentoEnviadoOutput>> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id);
        if (ordem == null)
            return OperationResult<OrcamentoEnviadoOutput>.Falha(TipoErro.NaoEncontrado, "Ordem de serviço não encontrada.");

        if (ordem.Status != StatusOrdemServico.EmDiagnostico)
            return OperationResult<OrcamentoEnviadoOutput>.Falha(
                TipoErro.OperacaoInvalida,
                $"A OS precisa estar Em Diagnóstico. Status atual: {ordem.Status}.");

        if (!ordem.Servicos.Any() && !ordem.Pecas.Any())
            return OperationResult<OrcamentoEnviadoOutput>.Falha(
                TipoErro.ValidacaoFalhou,
                "Não é possível enviar orçamento sem serviços ou peças.");

        ordem.AvancarStatus(); // EmDiagnostico → AguardandoAprovacao
        await _repository.SalvarAlteracoesAsync();

        await _eventPublisher.PublicarAsync(new OrcamentoEnviadoEvent
        {
            CorrelationId = ordem.Id,
            OrdemServicoId = ordem.Id,
            NumeroOS = ordem.NumeroOS,
            ValorTotal = ordem.ValorTotal
        });

        var output = new OrcamentoEnviadoOutput
        {
            Mensagem = "Orçamento enviado ao cliente para aprovação.",
            NumeroOS = ordem.NumeroOS,
            ValorTotal = ordem.ValorTotal,
            Status = ordem.Status.ToString(),
            Servicos = ordem.Servicos.Select(s => (object)new { s.Descricao, s.Preco }),
            Pecas = ordem.Pecas.Select(p => (object)new { p.Nome, p.Quantidade, p.PrecoUnitario })
        };

        return OperationResult<OrcamentoEnviadoOutput>.Ok(output);
    }
}
