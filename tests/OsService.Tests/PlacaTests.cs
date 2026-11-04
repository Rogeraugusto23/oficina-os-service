using FluentAssertions;
using OsService.Domain.ValueObjects;

namespace OsService.Tests;

public class PlacaTests
{
    [Theory]
    [InlineData("ABC1234")] // padrão antigo
    [InlineData("ABC1D23")] // Mercosul
    [InlineData("abc1234")] // minúsculo
    public void Validar_PlacaValida_DeveRetornarTrue(string placa)
    {
        Placa.Validar(placa).Should().BeTrue();
    }

    [Theory]
    [InlineData("INVALIDA")]
    [InlineData("AB1234")]
    [InlineData("")]
    [InlineData("1234ABC")]
    public void Validar_PlacaInvalida_DeveRetornarFalse(string placa)
    {
        Placa.Validar(placa).Should().BeFalse();
    }
}