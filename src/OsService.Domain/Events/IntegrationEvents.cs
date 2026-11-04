namespace OsService.Domain.Events;

/// <summary>
/// Eventos de integração publicados pelo OS Service para o barramento de mensageria
/// (SNS/SQS) como parte do Saga orquestrado da Fase 4.
///
/// Fluxo da Saga (orquestrado, coordenado pelo próprio OS Service):
///   1. AbrirOS                → OrdemServicoAbertaEvent        → Billing Service gera orçamento
///   2. Orçamento aprovado     → OrcamentoAprovadoEvent         → Execução Service inicia execução
///   3. Orçamento rejeitado    → OrcamentoRejeitadoEvent        → (fim da saga, OS cancelada)
///   4. Execução finalizada    → (consumido de ExecucaoFinalizadaEvent, publicado pelo Execução Service)
///   5. Falha em qualquer etapa → compensação (ex.: cancelar OS, estornar pagamento)
///
/// Cada evento carrega um CorrelationId igual ao Id da OrdemServico, usado para
/// rastrear a saga inteira através dos logs estruturados de todos os serviços
/// (reaproveitando o CorrelationIdMiddleware/Serilog já usados na Fase 3).
/// </summary>
public abstract record IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
}

public sealed record OrdemServicoAbertaEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public Guid ClienteId { get; init; }
    public Guid VeiculoId { get; init; }
}

public sealed record OrcamentoEnviadoEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public decimal ValorTotal { get; init; }
}

public sealed record OrcamentoAprovadoEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public decimal ValorTotal { get; init; }
}

public sealed record OrcamentoRejeitadoEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
}

public sealed record OrdemServicoCanceladaEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public string Motivo { get; init; } = "";
}

/// <summary>
/// Evento consumido (não publicado) pelo OS Service: o Execução Service notifica
/// que a execução da OS terminou (sucesso ou falha), permitindo que o OS Service
/// avance o status da OS ou execute a compensação da saga.
/// </summary>
public sealed record ExecucaoFinalizadaEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public bool Sucesso { get; init; }
    public string? MotivoFalha { get; init; }
}
