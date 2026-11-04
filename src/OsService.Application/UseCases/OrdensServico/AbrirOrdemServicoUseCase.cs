using OsService.Application.Common;
using OsService.Application.DTOs.OrdensServico;
using OsService.Domain.Events;
using OsService.Domain.Interfaces;
using DomainOrdemServico = OsService.Domain.Entities.OrdemServico;

namespace OsService.Application.UseCases.OrdensServico;

/// <summary>
/// Passo 1 da Saga orquestrada: ao abrir a OS, publica OrdemServicoAbertaEvent
/// no barramento de mensageria. O Billing Service escuta esse evento para gerar
/// o orçamento de forma assíncrona (desacoplando OS Service de Billing Service —
/// nenhum dos dois acessa o banco do outro diretamente).
/// </summary>
public class AbrirOrdemServicoUseCase
{
    private readonly IOrdemServicoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public AbrirOrdemServicoUseCase(IOrdemServicoRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<OperationResult<DomainOrdemServico>> ExecutarAsync(AbrirOrdemServicoInput input)
    {
        var novaOrdem = new DomainOrdemServico(input.ClienteId, input.VeiculoId);

        if (input.Servicos != null)
            foreach (var s in input.Servicos)
                novaOrdem.AdicionarServico(s.Descricao, s.Preco);

        if (input.Pecas != null)
            foreach (var p in input.Pecas)
                novaOrdem.AdicionarPeca(p.Nome, p.Quantidade, p.PrecoUnitario);

        await _repository.AdicionarAsync(novaOrdem);
        await _repository.SalvarAlteracoesAsync();

        await _eventPublisher.PublicarAsync(new OrdemServicoAbertaEvent
        {
            CorrelationId = novaOrdem.Id,
            OrdemServicoId = novaOrdem.Id,
            NumeroOS = novaOrdem.NumeroOS,
            ClienteId = novaOrdem.ClienteId,
            VeiculoId = novaOrdem.VeiculoId
        });

        return OperationResult<DomainOrdemServico>.Ok(novaOrdem, "Ordem de serviço aberta com sucesso.");
    }
}
