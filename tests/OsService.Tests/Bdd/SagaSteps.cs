using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OsService.Application.Saga;
using OsService.Application.UseCases.OrdensServico;
using OsService.Domain.Entities;
using OsService.Domain.Enums;
using OsService.Infrastructure.Data;
using OsService.Infrastructure.Repositories;
using OsService.Tests.Fakes;
using Reqnroll;

namespace OsService.Tests.Bdd;

[Binding]
public class SagaSteps
{
    private readonly OrdemServicoRepository _repo;
    private readonly FakeEventPublisher _publisher = new();
    private readonly SagaOrchestrator _saga;
    private OrdemServico _ordem = null!;

    public SagaSteps()
    {
        var options = new DbContextOptionsBuilder<OsServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _repo = new OrdemServicoRepository(new OsServiceDbContext(options));
        _saga = new SagaOrchestrator(_repo,
            new AprovarOrcamentoUseCase(_repo, _publisher),
            new RejeitarOrcamentoUseCase(_repo, _publisher),
            _publisher, NullLogger<SagaOrchestrator>.Instance);
    }

    [Given(@"uma ordem de serviço com status ""(.*)""")]
    public async Task DadoUmaOrdemComStatus(string status)
    {
        var alvo = Enum.Parse<StatusOrdemServico>(status);
        _ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        _ordem.AdicionarServico("Revisão", 200m);
        while (_ordem.Status < alvo) _ordem.AvancarStatus();
        await _repo.AdicionarAsync(_ordem);
        await _repo.SalvarAlteracoesAsync();
    }

    [When(@"o evento de pagamento aprovado é recebido")]
    public Task QuandoPagamentoAprovado() => _saga.OnPagamentoAprovadoAsync(_ordem.Id);

    [When(@"o evento de pagamento rejeitado é recebido com o motivo ""(.*)""")]
    public Task QuandoPagamentoRejeitado(string motivo) => _saga.OnPagamentoRejeitadoAsync(_ordem.Id, motivo);

    [When(@"a execução é finalizada com sucesso")]
    public Task QuandoExecucaoComSucesso() => _saga.OnExecucaoFinalizadaAsync(_ordem.Id, true, null);

    [When(@"a execução é finalizada com falha ""(.*)""")]
    public Task QuandoExecucaoComFalha(string motivo) => _saga.OnExecucaoFinalizadaAsync(_ordem.Id, false, motivo);

    [Then(@"a ordem de serviço deve estar com status ""(.*)""")]
    public async Task EntaoStatus(string esperado)
    {
        var atual = await _repo.ObterPorIdAsync(_ordem.Id);
        atual!.Status.Should().Be(Enum.Parse<StatusOrdemServico>(esperado));
    }

    [Then(@"o evento ""(.*)"" deve ter sido publicado")]
    public void EntaoEventoPublicado(string nomeEvento)
    {
        _publisher.EventosPublicados.Select(e => e.GetType().Name).Should().Contain(nomeEvento);
    }

    [Then(@"nenhum evento deve ter sido publicado")]
    public void EntaoNenhumEvento() => _publisher.EventosPublicados.Should().BeEmpty();
}
