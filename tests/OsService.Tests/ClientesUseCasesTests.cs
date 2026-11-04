using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OsService.Application.Common;
using OsService.Application.DTOs.Clientes;
using OsService.Application.UseCases.Clientes;
using OsService.Infrastructure.Data;
using OsService.Infrastructure.Repositories;

namespace OsService.Tests;

public class ClientesUseCasesTests
{
    private static OsServiceDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<OsServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OsServiceDbContext(options);
    }

    [Fact]
    public async Task CriarCliente_ComDocumentoValido_DeveCadastrarComoAtivo()
    {
        using var context = CriarContexto();
        var repository = new ClienteRepository(context);
        var useCase = new CriarClienteUseCase(repository);

        var resultado = await useCase.ExecutarAsync(new CriarClienteInput
        {
            Nome = "Maria Teste",
            Documento = "11144477735" // CPF válido (dígitos verificadores corretos)
        });

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task CriarCliente_ComDocumentoJaCadastrado_DeveFalhar()
    {
        using var context = CriarContexto();
        var repository = new ClienteRepository(context);
        var useCase = new CriarClienteUseCase(repository);

        await useCase.ExecutarAsync(new CriarClienteInput { Nome = "Cliente 1", Documento = "11144477735" });
        var resultado = await useCase.ExecutarAsync(new CriarClienteInput { Nome = "Cliente Duplicado", Documento = "11144477735" });

        resultado.Sucesso.Should().BeFalse();
        resultado.Erro.Should().Be(TipoErro.OperacaoInvalida);
    }

    [Fact]
    public async Task InativarCliente_DeveTornarAtivoFalse()
    {
        using var context = CriarContexto();
        var repository = new ClienteRepository(context);
        var criarUseCase = new CriarClienteUseCase(repository);
        var inativarUseCase = new InativarClienteUseCase(repository);

        var criado = await criarUseCase.ExecutarAsync(new CriarClienteInput { Nome = "Cliente X", Documento = "11144477735" });
        var resultado = await inativarUseCase.ExecutarAsync(criado.Dados!.Id);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.Ativo.Should().BeFalse();
    }
}
