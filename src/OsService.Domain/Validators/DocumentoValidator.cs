namespace OsService.Domain.Validators;

public static class DocumentoValidator
{
    public static bool ValidarCpf(string cpf)
    {
        cpf = new string(cpf.Where(char.IsDigit).ToArray());
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;

        int[] mult1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] mult2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var soma = mult1.Select((m, i) => m * (cpf[i] - '0')).Sum();
        var resto = soma % 11;
        var d1 = resto < 2 ? 0 : 11 - resto;

        soma = mult2.Select((m, i) => m * (cpf[i] - '0')).Sum();
        resto = soma % 11;
        var d2 = resto < 2 ? 0 : 11 - resto;

        return cpf[9] - '0' == d1 && cpf[10] - '0' == d2;
    }

    public static bool ValidarCnpj(string cnpj)
    {
        cnpj = new string(cnpj.Where(char.IsDigit).ToArray());
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1) return false;

        int[] mult1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] mult2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var soma = mult1.Select((m, i) => m * (cnpj[i] - '0')).Sum();
        var resto = soma % 11;
        var d1 = resto < 2 ? 0 : 11 - resto;

        soma = mult2.Select((m, i) => m * (cnpj[i] - '0')).Sum();
        resto = soma % 11;
        var d2 = resto < 2 ? 0 : 11 - resto;

        return cnpj[12] - '0' == d1 && cnpj[13] - '0' == d2;
    }

    public static bool Validar(string documento)
    {
        var digits = new string(documento.Where(char.IsDigit).ToArray());
        return digits.Length == 11 ? ValidarCpf(documento) : ValidarCnpj(documento);
    }
}