using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OsService.Application.Common;
using OsService.Application.DTOs.OrdensServico;
using OsService.Application.UseCases.OrdensServico;
using OsService.Domain.Entities;
using OsService.Domain.Enums;
using OsService.Infrastructure.Data;
using OsService.Infrastructure.Repositories;
using OsService.Tests.Fakes;

namespace OsService.Tests;

public class OrdensServicoUseCasesTests
{
    // Cada teste recebe um banco InMemory isolado (nome único por teste),
    // evitando que um teste influencie o outro.
    private static OsServiceDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<OsServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OsServiceDbContext(options);
    }

    [Fact]
    public async Task ListarOrdensServico_DeveExcluirFinalizadasEEntregues_EOrdenarPorPrioridade()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);
        var useCase = new ListarOrdensServicoUseCase(repository);

        var recebida = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        var emExecucao = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        emExecucao.AvancarStatus(); // Recebida -> EmDiagnostico
        emExecucao.AdicionarServico("Revisão", 100m);
        emExecucao.AvancarStatus(); // EmDiagnostico -> AguardandoAprovacao
        emExecucao.AvancarStatus(); // AguardandoAprovacao -> EmExecucao

        var finalizada = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        finalizada.AdicionarServico("Troca de óleo", 80m);
        finalizada.AvancarStatus(); // EmDiagnostico
        finalizada.AvancarStatus(); // AguardandoAprovacao
        finalizada.AvancarStatus(); // EmExecucao
        finalizada.AvancarStatus(); // Finalizada

        await repository.AdicionarAsync(recebida);
        await repository.AdicionarAsync(emExecucao);
        await repository.AdicionarAsync(finalizada);
        await repository.SalvarAlteracoesAsync();

        var resultado = await useCase.ExecutarAsync();

        resultado.Sucesso.Should().BeTrue();
        var lista = resultado.Dados!.ToList();

        // A finalizada não deve aparecer (exclusão lógica na listagem)
        lista.Should().NotContain(o => o.Id == finalizada.Id);

        // EmExecucao tem prioridade sobre Recebida
        lista.Select(o => o.Id).Should().ContainInOrder(emExecucao.Id, recebida.Id);
    }

    [Fact]
    public async Task AprovarOrcamento_QuandoAguardandoAprovacao_DeveAvancarParaEmExecucao()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);

        var ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        ordem.AdicionarServico("Alinhamento", 120m);
        ordem.AvancarStatus(); // EmDiagnostico
        ordem.AvancarStatus(); // AguardandoAprovacao

        await repository.AdicionarAsync(ordem);
        await repository.SalvarAlteracoesAsync();

        var useCase = new AprovarOrcamentoUseCase(repository, new FakeEventPublisher());
        var resultado = await useCase.ExecutarAsync(ordem.Id);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Status.Should().Be(StatusOrdemServico.EmExecucao.ToString());
    }

    [Fact]
    public async Task AprovarOrcamento_QuandoStatusIncompativel_DeveFalharComOperacaoInvalida()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);

        // OS recém-criada está em "Recebida", não em "AguardandoAprovacao"
        var ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        await repository.AdicionarAsync(ordem);
        await repository.SalvarAlteracoesAsync();

        var useCase = new AprovarOrcamentoUseCase(repository, new FakeEventPublisher());
        var resultado = await useCase.ExecutarAsync(ordem.Id);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.OperacaoInvalida);
    }

    [Fact]
    public async Task RejeitarOrcamento_QuandoAguardandoAprovacao_DeveCancelarOS()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);

        var ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        ordem.AdicionarServico("Freios", 200m);
        ordem.AvancarStatus(); // EmDiagnostico
        ordem.AvancarStatus(); // AguardandoAprovacao

        await repository.AdicionarAsync(ordem);
        await repository.SalvarAlteracoesAsync();

        var useCase = new RejeitarOrcamentoUseCase(repository, new FakeEventPublisher());
        var resultado = await useCase.ExecutarAsync(ordem.Id);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Status.Should().Be(StatusOrdemServico.Cancelada.ToString());
    }

    [Fact]
    public async Task AtualizarStatusPorEmail_ComNumeroOSInexistente_DeveFalharComNaoEncontrado()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);
        var useCase = new AtualizarStatusPorEmailUseCase(repository);

        var resultado = await useCase.ExecutarAsync(new AtualizacaoStatusEmailInput
        {
            NumeroOS = "NAO-EXISTE-123",
            NovoStatus = "EmExecucao",
            RemetenteEmail = "cliente@teste.com"
        });

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task AtualizarStatusPorEmail_ComStatusValido_DeveAtualizarOStatusDaOS()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);

        var ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        await repository.AdicionarAsync(ordem);
        await repository.SalvarAlteracoesAsync();

        var useCase = new AtualizarStatusPorEmailUseCase(repository);
        var resultado = await useCase.ExecutarAsync(new AtualizacaoStatusEmailInput
        {
            NumeroOS = ordem.NumeroOS,
            NovoStatus = "EmExecucao",
            RemetenteEmail = "cliente@teste.com"
        });

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.StatusAtual.Should().Be("EmExecucao");
    }

    [Fact]
    public async Task AtualizarStatusPorEmail_ComStatusTextoInvalido_DeveFalharComValidacao()
    {
        using var context = CriarContexto();
        var repository = new OrdemServicoRepository(context);

        var ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        await repository.AdicionarAsync(ordem);
        await repository.SalvarAlteracoesAsync();

        var useCase = new AtualizarStatusPorEmailUseCase(repository);
        var resultado = await useCase.ExecutarAsync(new AtualizacaoStatusEmailInput
        {
            NumeroOS = ordem.NumeroOS,
            NovoStatus = "StatusQueNaoExiste"
        });

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.ValidacaoFalhou);
    }
}
