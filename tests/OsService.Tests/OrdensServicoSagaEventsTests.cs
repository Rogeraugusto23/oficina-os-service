using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OsService.Application.DTOs.OrdensServico;
using OsService.Application.UseCases.OrdensServico;
using OsService.Domain.Events;
using OsService.Infrastructure.Data;
using OsService.Infrastructure.Repositories;
using OsService.Tests.Fakes;

namespace OsService.Tests;

/// <summary>
/// Cobre a publicação dos eventos de integração que disparam a Saga
/// orquestrada entre OS Service, Billing Service e Execução Service.
/// </summary>
public class OrdensServicoSagaEventsTests
{
    private static OsServiceDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<OsServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OsServiceDbContext(options);
    }

    [Fact]
    public async Task AbrirOrdemServico_DevePublicarOrdemServicoAbertaEvent()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);
        var publisher = new FakeEventPublisher();
        var useCase = new AbrirOrdemServicoUseCase(repository, publisher);

        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var resultado = await useCase.ExecutarAsync(new AbrirOrdemServicoInput
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        });

        resultado.Sucesso.Should().BeTrue();
        publisher.EventosPublicados.Should().ContainSingle()
            .Which.Should().BeOfType<OrdemServicoAbertaEvent>();

        var evento = (OrdemServicoAbertaEvent)publisher.EventosPublicados.Single();
        evento.OrdemServicoId.Should().Be(resultado.Dados!.Id);
        evento.ClienteId.Should().Be(clienteId);
        evento.VeiculoId.Should().Be(veiculoId);
        evento.CorrelationId.Should().Be(resultado.Dados!.Id);
    }

    [Fact]
    public async Task EnviarOrcamento_DevePublicarOrcamentoEnviadoEvent_ComValorTotalCorreto()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);
        var publisher = new FakeEventPublisher();

        var ordem = new OsService.Domain.Entities.OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        ordem.AvancarStatus(); // Recebida -> EmDiagnostico
        ordem.AdicionarServico("Troca de óleo", 150m);
        await repository.AdicionarAsync(ordem);
        await repository.SalvarAlteracoesAsync();

        var useCase = new EnviarOrcamentoUseCase(repository, publisher);
        var resultado = await useCase.ExecutarAsync(ordem.Id);

        resultado.Sucesso.Should().BeTrue();
        var evento = publisher.EventosPublicados.OfType<OrcamentoEnviadoEvent>().Should().ContainSingle().Subject;
        evento.ValorTotal.Should().Be(150m);
        evento.NumeroOS.Should().Be(ordem.NumeroOS);
    }

    [Fact]
    public async Task AprovarOrcamento_DevePublicarOrcamentoAprovadoEvent()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);
        var publisher = new FakeEventPublisher();

        var ordem = new OsService.Domain.Entities.OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        ordem.AdicionarServico("Revisão", 300m);
        ordem.AvancarStatus(); // EmDiagnostico
        ordem.AvancarStatus(); // AguardandoAprovacao
        await repository.AdicionarAsync(ordem);
        await repository.SalvarAlteracoesAsync();

        var useCase = new AprovarOrcamentoUseCase(repository, publisher);
        var resultado = await useCase.ExecutarAsync(ordem.Id);

        resultado.Sucesso.Should().BeTrue();
        publisher.EventosPublicados.Should().ContainSingle().Which.Should().BeOfType<OrcamentoAprovadoEvent>();
    }

    [Fact]
    public async Task RejeitarOrcamento_DevePublicarOrcamentoRejeitadoEvent()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);
        var publisher = new FakeEventPublisher();

        var ordem = new OsService.Domain.Entities.OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        ordem.AdicionarServico("Suspensão", 400m);
        ordem.AvancarStatus(); // EmDiagnostico
        ordem.AvancarStatus(); // AguardandoAprovacao
        await repository.AdicionarAsync(ordem);
        await repository.SalvarAlteracoesAsync();

        var useCase = new RejeitarOrcamentoUseCase(repository, publisher);
        var resultado = await useCase.ExecutarAsync(ordem.Id);

        resultado.Sucesso.Should().BeTrue();
        publisher.EventosPublicados.Should().ContainSingle().Which.Should().BeOfType<OrcamentoRejeitadoEvent>();
    }
}
