namespace OsService.Application.Common;

public enum TipoErro
{
    Nenhum,
    NaoEncontrado,
    ValidacaoFalhou,
    OperacaoInvalida,
    NaoAutorizado
}

/// <summary>
/// Resultado de um Use Case. Mantém a camada de Application livre de qualquer
/// dependência de ASP.NET Core (ActionResult, StatusCode, etc.) — quem traduz
/// isso para HTTP é o Controller.
/// </summary>
public class OperationResult<T>
{
    public bool Sucesso { get; }
    public T? Dados { get; }
    public string? Mensagem { get; }
    public TipoErro Erro { get; }

    private OperationResult(bool sucesso, T? dados, string? mensagem, TipoErro erro)
    {
        Sucesso = sucesso;
        Dados = dados;
        Mensagem = mensagem;
        Erro = erro;
    }

    public static OperationResult<T> Ok(T dados, string? mensagem = null) =>
        new(true, dados, mensagem, TipoErro.Nenhum);

    public static OperationResult<T> Falha(TipoErro erro, string mensagem) =>
        new(false, default, mensagem, erro);
}
