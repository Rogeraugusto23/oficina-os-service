using FluentAssertions;
using OsService.Domain.Entities;
using OsService.Domain.Enums;

namespace OsService.Tests;

public class OrdemServicoTests
{
    [Fact]
    public void CriarOS_DeveGerarNumeroOS_EStatusRecebida()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.NumeroOS.Should().NotBeNullOrEmpty();
        os.Status.Should().Be(StatusOrdemServico.Recebida);
        os.ValorTotal.Should().Be(0);
    }

    [Fact]
    public void AdicionarServico_DeveCalcularValorTotal()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.AdicionarServico("Troca de Óleo", 150.50m);

        os.Servicos.Should().HaveCount(1);
        os.ValorTotal.Should().Be(150.50m);
    }

    [Fact]
    public void AdicionarPeca_DeveCalcularValorTotal()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.AdicionarPeca("Filtro de Óleo", 2, 45.00m);

        os.Pecas.Should().HaveCount(1);
        os.ValorTotal.Should().Be(90.00m);
    }

    [Fact]
    public void AdicionarServicoeEPeca_DeveCalcularTotalCombinado()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.AdicionarServico("Troca de Óleo", 150.50m);
        os.AdicionarPeca("Filtro de Óleo", 1, 45.00m);

        os.ValorTotal.Should().Be(195.50m);
    }

    [Fact]
    public void AvancarStatus_DeveIncrementarStatus()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.AvancarStatus();

        os.Status.Should().Be(StatusOrdemServico.EmDiagnostico);
    }

    [Fact]
    public void AvancarStatus_NaoDevePassarDeEntregue()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        // Avança até o fim
        for (int i = 0; i < 10; i++) os.AvancarStatus();

        os.Status.Should().Be(StatusOrdemServico.Entregue);
    }
}