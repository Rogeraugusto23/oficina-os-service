using OsService.Application.Common;
using OsService.Domain.Events;

namespace OsService.Tests.Fakes;

/// <summary>
/// Dublê de teste para IEventPublisher — evita qualquer dependência de AWS/SNS
/// nos testes de unidade dos Use Cases, apenas registra os eventos publicados
/// para que os testes possam fazer asserções sobre eles quando necessário.
/// </summary>
public class FakeEventPublisher : IEventPublisher
{
    public List<IntegrationEvent> EventosPublicados { get; } = new();

    public Task PublicarAsync<TEvent>(TEvent evento) where TEvent : IntegrationEvent
    {
        EventosPublicados.Add(evento);
        return Task.CompletedTask;
    }
}
