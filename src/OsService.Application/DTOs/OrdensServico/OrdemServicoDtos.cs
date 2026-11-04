namespace OsService.Application.DTOs.OrdensServico;

// ---------- Entrada ----------

public class AbrirOrdemServicoInput
{
    public Guid ClienteId { get; set; }
    public Guid VeiculoId { get; set; }
    public List<ServicoInput>? Servicos { get; set; }
    public List<PecaInput>? Pecas { get; set; }
}

public class ServicoInput
{
    public string Descricao { get; set; } = "";
    public decimal Preco { get; set; }
}

public class PecaInput
{
    public string Nome { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public class AtualizacaoStatusEmailInput
{
    public string NumeroOS { get; set; } = "";
    public string NovoStatus { get; set; } = "";
    public string? RemetenteEmail { get; set; }
    public string? AssuntoOriginal { get; set; }
}

// ---------- Saída ----------

public class OrcamentoEnviadoOutput
{
    public string Mensagem { get; set; } = "";
    public string NumeroOS { get; set; } = "";
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = "";
    public IEnumerable<object> Servicos { get; set; } = Array.Empty<object>();
    public IEnumerable<object> Pecas { get; set; } = Array.Empty<object>();
}

public class StatusAtualizadoOutput
{
    public string Mensagem { get; set; } = "";
    public string NumeroOS { get; set; } = "";
    public string Status { get; set; } = "";
}

public class StatusAtualizadoPorEmailOutput
{
    public string Mensagem { get; set; } = "";
    public string NumeroOS { get; set; } = "";
    public string StatusAnterior { get; set; } = "";
    public string StatusAtual { get; set; } = "";
    public string? OrigemEmail { get; set; }
}
