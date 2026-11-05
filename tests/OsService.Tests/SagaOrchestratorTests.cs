using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OsService.Application.Saga;
using OsService.Application.UseCases.OrdensServico;
using OsService.Domain.Entities;
using OsService.Domain.Enums;
using OsService.Domain.Events;
using OsService.Infrastructure.Data;
using OsService.Infrastructure.Repositories;
using OsService.Tests.Fakes;

namespace OsService.Tests;

public class SagaOrchestratorTests
{
    private static (SagaOrchestrator saga, OrdemServicoRepository repo, FakeEventPublisher pub) Criar()
    {
        var options = new DbContextOptionsBuilder<OsServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var repo = new OrdemServicoRepository(new OsServiceDbContext(options));
        var pub = new FakeEventPublisher();
        var saga = new SagaOrchestrator(repo,
            new AprovarOrcamentoUseCase(repo, pub),
            new RejeitarOrcamentoUseCase(repo, pub),
            pub, NullLogger<SagaOrchestrator>.Instance);
        return (saga, repo, pub);
    }

    private static async Task<OrdemServico> OsEm(OrdemServicoRepository repo, StatusOrdemServico alvo)
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.AdicionarServico("Revisão", 200m);
        while (os.Status < alvo) os.AvancarStatus();
        await repo.AdicionarAsync(os);
        await repo.SalvarAlteracoesAsync();
        return os;
    }

    [Fact]
    public async Task PagamentoAprovado_DeveAprovarOrcamento_EPublicarEventoParaExecucao()
    {
        var (saga, repo, pub) = Criar();
        var os = await OsEm(repo, StatusOrdemServico.AguardandoAprovacao);

        await saga.OnPagamentoAprovadoAsync(os.Id);

        (await repo.ObterPorIdAsync(os.Id))!.Status.Should().Be(StatusOrdemServico.EmExecucao);
        pub.EventosPublicados.Should().ContainSingle().Which.Should().BeOfType<OrcamentoAprovadoEvent>();
    }

    [Fact]
    public async Task PagamentoAprovado_Duplicado_DeveSerIdempotente()
    {
        var (saga, repo, pub) = Criar();
        var os = await OsEm(repo, StatusOrdemServico.AguardandoAprovacao);

        await saga.OnPagamentoAprovadoAsync(os.Id);
        await saga.OnPagamentoAprovadoAsync(os.Id);

        pub.EventosPublicados.Should().HaveCount(1);
    }

    [Fact]
    public async Task PagamentoRejeitado_DeveCancelarOS_Compensacao()
    {
        var (saga, repo, pub) = Criar();
        var os = await OsEm(repo, StatusOrdemServico.AguardandoAprovacao);

        await saga.OnPagamentoRejeitadoAsync(os.Id, "Cartão recusado");

        (await repo.ObterPorIdAsync(os.Id))!.Status.Should().Be(StatusOrdemServico.Cancelada);
        pub.EventosPublicados.Should().ContainSingle().Which.Should().BeOfType<OrcamentoRejeitadoEvent>();
    }

    [Fact]
    public async Task ExecucaoFinalizada_ComSucesso_DeveFinalizarOS()
    {
        var (saga, repo, pub) = Criar();
        var os = await OsEm(repo, StatusOrdemServico.EmExecucao);

        await saga.OnExecucaoFinalizadaAsync(os.Id, true, null);

        (await repo.ObterPorIdAsync(os.Id))!.Status.Should().Be(StatusOrdemServico.Finalizada);
        pub.EventosPublicados.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecucaoFinalizada_ComFalha_DeveCancelarOS_EPedirEstornoAoBilling()
    {
        var (saga, repo, pub) = Criar();
        var os = await OsEm(repo, StatusOrdemServico.EmExecucao);

        await saga.OnExecucaoFinalizadaAsync(os.Id, false, "Peça indisponível");

        (await repo.ObterPorIdAsync(os.Id))!.Status.Should().Be(StatusOrdemServico.Cancelada);
        var ev = pub.EventosPublicados.Should().ContainSingle().Subject.Should().BeOfType<OrdemServicoCanceladaEvent>().Subject;
        ev.Motivo.Should().Be("Peça indisponível");
    }

    [Fact]
    public async Task EventoParaOsInexistente_DeveSerIgnorado()
    {
        var (saga, _, pub) = Criar();
        await saga.OnExecucaoFinalizadaAsync(Guid.NewGuid(), false, "x");
        pub.EventosPublicados.Should().BeEmpty();
    }
}
