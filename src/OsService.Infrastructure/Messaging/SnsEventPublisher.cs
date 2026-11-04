using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsService.Application.Common;
using OsService.Domain.Events;

namespace OsService.Infrastructure.Messaging;

/// <summary>
/// Publica eventos de integração em um tópico SNS (fan-out para as filas SQS do
/// Billing Service e do Execução Service — cada serviço com sua própria
/// subscription/fila, sem acoplamento direto entre eles). O nome do tipo do
/// evento vai como atributo de mensagem ("EventType"), permitindo que cada
/// consumidor filtre apenas os eventos que lhe interessam.
/// </summary>
public class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly ILogger<SnsEventPublisher> _logger;
    private readonly string? _topicArn;

    public SnsEventPublisher(IAmazonSimpleNotificationService sns, IConfiguration config, ILogger<SnsEventPublisher> logger)
    {
        _sns = sns;
        _logger = logger;
        _topicArn = config["Messaging:SnsTopicArn"];
    }

    public async Task PublicarAsync<TEvent>(TEvent evento) where TEvent : IntegrationEvent
    {
        var tipoEvento = typeof(TEvent).Name;
        var payload = JsonSerializer.Serialize(evento);

        if (string.IsNullOrWhiteSpace(_topicArn))
        {
            // Sem tópico configurado (ex.: ambiente local sem AWS) — loga e segue,
            // em vez de quebrar o fluxo principal de negócio.
            _logger.LogWarning(
                "Messaging:SnsTopicArn não configurado — evento {EventType} (CorrelationId={CorrelationId}) não foi publicado: {Payload}",
                tipoEvento, evento.CorrelationId, payload);
            return;
        }

        try
        {
            await _sns.PublishAsync(new PublishRequest
            {
                TopicArn = _topicArn,
                Message = payload,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["EventType"] = new MessageAttributeValue { DataType = "String", StringValue = tipoEvento }
                }
            });

            _logger.LogInformation(
                "Evento {EventType} publicado no SNS (CorrelationId={CorrelationId}, NumeroOS referenciado no payload)",
                tipoEvento, evento.CorrelationId);
        }
        catch (Exception ex)
        {
            // A publicação do evento não deve derrubar a transação de negócio já
            // persistida (ex.: a OS já foi aberta no banco). Logamos o erro para
            // investigação/retry manual — numa evolução futura isso vira uma
            // tabela de outbox com reprocessamento automático.
            _logger.LogError(ex,
                "Falha ao publicar evento {EventType} no SNS (CorrelationId={CorrelationId})",
                tipoEvento, evento.CorrelationId);
        }
    }
}
