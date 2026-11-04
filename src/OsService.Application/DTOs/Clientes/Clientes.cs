namespace OsService.Application.DTOs.Clientes;

public class CriarClienteInput
{
    public string Nome { get; set; } = "";
    public string Documento { get; set; } = "";
}

public class ClienteOutput
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Documento { get; set; } = "";
    public bool Ativo { get; set; }
}