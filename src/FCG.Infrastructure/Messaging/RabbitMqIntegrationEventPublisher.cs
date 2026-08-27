using System.Collections.Concurrent;
using System.Reflection;
using FCG.Application.Messaging;
using FiapCloudGames.Contracts;
using FiapCloudGames.RabbitMq.Publishers;
using Microsoft.Extensions.Logging;

namespace FCG.Infrastructure.Messaging;

/// <summary>
/// Adapta o IRabbitMqPublisher (pacote FiapCloudGames.RabbitMq) para o contrato
/// IIntegrationEventPublisher da camada de Application. A rota (exchange/routing key)
/// é resolvida a partir do atributo [IntegrationEventRoute] do próprio evento.
/// </summary>
public sealed class RabbitMqIntegrationEventPublisher(
    IRabbitMqPublisher publisher,
    ILogger<RabbitMqIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    // Reflection só na primeira publicação de cada tipo de evento; depois vem do cache.
    private static readonly ConcurrentDictionary<Type, (string Exchange, string RoutingKey)> RouteCache = new();

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Usa o tipo em runtime (e não typeof(TEvent)) para achar a rota mesmo quando
        // o evento é publicado por uma referência do tipo base IIntegrationEvent.
        var (exchange, routingKey) = ResolveRoute(@event.GetType());

        try
        {
            await publisher.PublishAsync(exchange, routingKey, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            // Trade-off consciente: publicamos após o CommitAsync, sem padrão Outbox. O
            // IRabbitMqPublisher já loga o erro de infraestrutura antes de relançar; aqui
            // capturamos para não falhar a operação de negócio (o usuário já foi persistido).
            // Em produção, o Outbox garantiria a entrega do evento mesmo com o broker fora do ar.
            logger.LogWarning(
                ex,
                "Evento {EventType} (EventId {EventId}) não pôde ser publicado e será perdido (sem Outbox)",
                @event.GetType().Name,
                @event.EventId);
        }
    }

    private static (string Exchange, string RoutingKey) ResolveRoute(Type eventType) =>
        RouteCache.GetOrAdd(eventType, static type =>
        {
            var route = type.GetCustomAttribute<IntegrationEventRouteAttribute>()
                ?? throw new InvalidOperationException(
                    $"O evento {type.Name} não possui [IntegrationEventRoute]. " +
                    "Anote-o no pacote FiapCloudGames.Contracts com a exchange e a routing key.");

            return (route.Exchange, route.RoutingKey);
        });
}
