using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OsService.Application.Saga;
using OsService.Domain.Events;

namespace OsService.Infrastructure.Messaging;

/// <summary>
/// Consome a fila do OS Service e repassa ao SagaOrchestrator os eventos
/// vindos do Billing (pagamento) e do Execução (finalização).
/// </summary>
public class SqsEventConsumerBackgroundService : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SqsEventConsumerBackgroundService> _logger;
    private readonly string? _queueUrl;

    public SqsEventConsumerBackgroundService(IAmazonSQS sqs, IServiceScopeFactory scopeFactory,
        IConfiguration config, ILogger<SqsEventConsumerBackgroundService> logger)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueUrl = config["Messaging:SqsQueueUrl"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_queueUrl))
        {
            _logger.LogWarning("Messaging:SqsQueueUrl não configurado — consumidor da Saga desligado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 10,
                    MessageAttributeNames = new List<string> { "EventType" }
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await ProcessarAsync(message);
                    await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consumir fila SQS da Saga");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessarAsync(Message message)
    {
        if (!message.MessageAttributes.TryGetValue("EventType", out var tipo))
        {
            _logger.LogWarning("Mensagem SQS sem EventType ignorada: {MessageId}", message.MessageId);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var saga = scope.ServiceProvider.GetRequiredService<SagaOrchestrator>();

        switch (tipo.StringValue)
        {
            case nameof(PagamentoAprovadoEvent):
                await saga.OnPagamentoAprovadoAsync(JsonSerializer.Deserialize<PagamentoAprovadoEvent>(message.Body)!.OrdemServicoId);
                break;
            case nameof(PagamentoRejeitadoEvent):
                var rej = JsonSerializer.Deserialize<PagamentoRejeitadoEvent>(message.Body)!;
                await saga.OnPagamentoRejeitadoAsync(rej.OrdemServicoId, rej.Motivo);
                break;
            case nameof(ExecucaoFinalizadaEvent):
                var fin = JsonSerializer.Deserialize<ExecucaoFinalizadaEvent>(message.Body)!;
                await saga.OnExecucaoFinalizadaAsync(fin.OrdemServicoId, fin.Sucesso, fin.MotivoFalha);
                break;
            default:
                _logger.LogDebug("Evento {EventType} ignorado pelo OS Service", tipo.StringValue);
                break;
        }
    }
}
