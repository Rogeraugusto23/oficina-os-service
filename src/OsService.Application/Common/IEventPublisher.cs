using OsService.Domain.Events;

namespace OsService.Application.Common;

/// <summary>
/// Abstração de publicação de eventos de integração. A camada de Application
/// depende apenas deste contrato — nunca do SDK da AWS diretamente — para que
/// os Use Cases permaneçam testáveis sem infraestrutura real (SNS/SQS).
/// A implementação concreta (SnsEventPublisher) vive em Infrastructure/Messaging.
/// </summary>
public interface IEventPublisher
{
    Task PublicarAsync<TEvent>(TEvent evento) where TEvent : IntegrationEvent;
}
