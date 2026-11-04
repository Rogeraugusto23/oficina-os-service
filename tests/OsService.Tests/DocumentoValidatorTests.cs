using FluentAssertions;
using OsService.Domain.Validators;

namespace OsService.Tests;

public class DocumentoValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25")] // CPF válido formatado
    [InlineData("52998224725")]    // CPF válido sem formatação
    public void ValidarCpf_Valido_DeveRetornarTrue(string cpf)
    {
        DocumentoValidator.ValidarCpf(cpf).Should().BeTrue();
    }

    [Theory]
    [InlineData("111.111.111-11")] // CPF com dígitos repetidos
    [InlineData("123.456.789-00")] // CPF inválido
    [InlineData("")]               // vazio
    public void ValidarCpf_Invalido_DeveRetornarFalse(string cpf)
    {
        DocumentoValidator.ValidarCpf(cpf).Should().BeFalse();
    }

    [Theory]
    [InlineData("11.222.333/0001-81")] // CNPJ válido formatado
    [InlineData("11222333000181")]     // CNPJ válido sem formatação
    public void ValidarCnpj_Valido_DeveRetornarTrue(string cnpj)
    {
        DocumentoValidator.ValidarCnpj(cnpj).Should().BeTrue();
    }

    [Theory]
    [InlineData("11.111.111/1111-11")] // CNPJ com dígitos repetidos
    [InlineData("00.000.000/0000-00")] // CNPJ inválido
    public void ValidarCnpj_Invalido_DeveRetornarFalse(string cnpj)
    {
        DocumentoValidator.ValidarCnpj(cnpj).Should().BeFalse();
    }
}