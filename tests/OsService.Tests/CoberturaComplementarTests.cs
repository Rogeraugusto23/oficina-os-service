using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OsService.Application.Common;
using OsService.Application.UseCases.Clientes;
using OsService.Application.UseCases.OrdensServico;
using OsService.Domain.Entities;
using OsService.Domain.Enums;
using OsService.Domain.Events;
using OsService.Domain.ValueObjects;
using OsService.Infrastructure.Data;
using OsService.Infrastructure.Repositories;

namespace OsService.Tests;

public class CoberturaComplementarTests
{
    private const string CpfValido = "11144477735";

    private static OsServiceDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<OsServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OsServiceDbContext(options);
    }

    // ---------- Veiculo ----------

    [Fact]
    public void Veiculo_Valido_DeveNormalizarPlaca()
    {
        var clienteId = Guid.NewGuid();
        var v = new Veiculo("abc-1d23", "Fiat", "Uno", 2020, clienteId);

        v.Placa.Should().Be("ABC1D23");
        v.ClienteId.Should().Be(clienteId);
        v.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("XXXX", "Fiat", "Uno", 2020)]
    [InlineData("ABC1D23", " ", "Uno", 2020)]
    [InlineData("ABC1D23", "Fiat", "", 2020)]
    [InlineData("ABC1D23", "Fiat", "Uno", 1800)]
    public void Veiculo_ComDadosInvalidos_DeveLancarArgumentException(string placa, string marca, string modelo, int ano)
    {
        var act = () => new Veiculo(placa, marca, modelo, ano, Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Veiculo_Atualizar_DeveAlterarDados()
    {
        var v = new Veiculo("ABC1D23", "Fiat", "Uno", 2020, Guid.NewGuid());

        v.Atualizar("VW", "Gol", 2022);

        v.Marca.Should().Be("VW");
        v.Modelo.Should().Be("Gol");
        v.Ano.Should().Be(2022);
    }

    [Theory]
    [InlineData("", "Gol", 2022)]
    [InlineData("VW", "", 2022)]
    [InlineData("VW", "Gol", 1800)]
    public void Veiculo_AtualizarComDadosInvalidos_DeveLancarArgumentException(string marca, string modelo, int ano)
    {
        var v = new Veiculo("ABC1D23", "Fiat", "Uno", 2020, Guid.NewGuid());
        var act = () => v.Atualizar(marca, modelo, ano);
        act.Should().Throw<ArgumentException>();
    }

    // ---------- Value objects ----------

    [Fact]
    public void Cpf_Valido_DeveFormatar()
    {
        var cpf = new Cpf(CpfValido);
        cpf.Numero.Should().Be(CpfValido);
        cpf.ToString().Should().Be("111.444.777-35");
        Cpf.Validar(CpfValido).Should().BeTrue();
    }

    [Fact]
    public void Cpf_Invalido_DeveLancarArgumentException()
    {
        var act = () => new Cpf("11111111111");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Placa_ValidaEInvalida()
    {
        new Placa("abc-1234").ToString().Should().Be("ABC-1234");
        Placa.Validar("").Should().BeFalse();
        var act = () => new Placa("123");
        act.Should().Throw<ArgumentException>();
    }

    // ---------- Cliente ----------

    [Fact]
    public void Cliente_DeveInativarReativarEAtualizarNome()
    {
        var c = new Cliente("Maria", CpfValido);

        c.Inativar();
        c.Ativo.Should().BeFalse();
        c.Reativar();
        c.Ativo.Should().BeTrue();
        c.AtualizarNome("Maria Silva");
        c.Nome.Should().Be("Maria Silva");
    }

    [Fact]
    public void Cliente_ComDadosInvalidos_DeveLancarArgumentException()
    {
        var semNome = () => new Cliente(" ", CpfValido);
        var docInvalido = () => new Cliente("Maria", "123");
        var nomeVazio = () => new Cliente("Maria", CpfValido).AtualizarNome("");

        semNome.Should().Throw<ArgumentException>();
        docInvalido.Should().Throw<ArgumentException>();
        nomeVazio.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ConsultarCliente_ExistenteENaoExistente()
    {
        using var context = CriarContexto();
        var repo = new ClienteRepository(context);
        var cliente = new Cliente("Maria", CpfValido);
        await repo.AdicionarAsync(cliente);
        await repo.SalvarAlteracoesAsync();
        var useCase = new ConsultarClienteUseCase(repo);

        (await useCase.ExecutarAsync(cliente.Id)).Sucesso.Should().BeTrue();
        var naoEncontrado = await useCase.ExecutarAsync(Guid.NewGuid());
        naoEncontrado.Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task ReativarCliente_DeveReativarEtratarInexistente()
    {
        using var context = CriarContexto();
        var repo = new ClienteRepository(context);
        var cliente = new Cliente("Maria", CpfValido);
        cliente.Inativar();
        await repo.AdicionarAsync(cliente);
        await repo.SalvarAlteracoesAsync();
        var useCase = new ReativarClienteUseCase(repo);

        var ok = await useCase.ExecutarAsync(cliente.Id);
        ok.Sucesso.Should().BeTrue();
        ok.Dados!.Ativo.Should().BeTrue();
        (await useCase.ExecutarAsync(Guid.NewGuid())).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---------- Ordem de serviço ----------

    [Fact]
    public async Task AvancarStatus_DeveAvancarEFalharQuandoInexistente()
    {
        using var context = CriarContexto();
        var repo = new OrdemServicoRepository(context);
        var ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        await repo.AdicionarAsync(ordem);
        await repo.SalvarAlteracoesAsync();
        var useCase = new AvancarStatusUseCase(repo);

        var ok = await useCase.ExecutarAsync(ordem.Id);
        ok.Sucesso.Should().BeTrue();
        ok.Dados!.Status.Should().Be(StatusOrdemServico.EmDiagnostico);
        (await useCase.ExecutarAsync(Guid.NewGuid())).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    [Fact]
    public async Task AvancarStatus_QuandoOSCancelada_DeveRetornarOperacaoInvalida()
    {
        using var context = CriarContexto();
        var repo = new OrdemServicoRepository(context);
        var ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        ordem.CancelarOS();
        await repo.AdicionarAsync(ordem);
        await repo.SalvarAlteracoesAsync();

        var resultado = await new AvancarStatusUseCase(repo).ExecutarAsync(ordem.Id);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.OperacaoInvalida);
    }

    [Fact]
    public async Task ConsultarOrdemServico_ExistenteENaoExistente()
    {
        using var context = CriarContexto();
        var repo = new OrdemServicoRepository(context);
        var ordem = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        await repo.AdicionarAsync(ordem);
        await repo.SalvarAlteracoesAsync();
        var useCase = new ConsultarOrdemServicoUseCase(repo);

        (await useCase.ExecutarAsync(ordem.Id)).Sucesso.Should().BeTrue();
        (await useCase.ExecutarAsync(Guid.NewGuid())).Erro.Should().Be(TipoErro.NaoEncontrado);
    }

    // ---------- Contratos de evento ----------

    [Fact]
    public void EventosConsumidos_DevemPreservarPropriedades()
    {
        var id = Guid.NewGuid();
        var pago = new PagamentoAprovadoEvent { OrdemServicoId = id, MercadoPagoPaymentId = "PAY-1" };
        var recusado = new PagamentoRejeitadoEvent { OrdemServicoId = id, Motivo = "Cartao recusado" };
        var exec = new ExecucaoFinalizadaEvent { OrdemServicoId = id, Sucesso = false, MotivoFalha = "Peca" };

        pago.OrdemServicoId.Should().Be(id);
        pago.MercadoPagoPaymentId.Should().Be("PAY-1");
        recusado.Motivo.Should().Be("Cartao recusado");
        exec.Sucesso.Should().BeFalse();
        exec.MotivoFalha.Should().Be("Peca");
        exec.OcorridoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
