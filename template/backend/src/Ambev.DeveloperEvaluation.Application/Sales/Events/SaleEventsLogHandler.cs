using Ambev.DeveloperEvaluation.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events;

/// <summary>
/// Publishes sale domain events to the application log.
/// Stand-in for a message broker: replace or complement with a Rebus/broker publisher when needed.
/// </summary>
public class SaleEventsLogHandler :
    INotificationHandler<SaleCreatedEvent>,
    INotificationHandler<SaleModifiedEvent>,
    INotificationHandler<SaleCancelledEvent>,
    INotificationHandler<ItemCancelledEvent>
{
    private readonly ILogger<SaleEventsLogHandler> _logger;

    public SaleEventsLogHandler(ILogger<SaleEventsLogHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SaleCreatedEvent notification, CancellationToken cancellationToken) =>
        Log(nameof(SaleCreatedEvent), notification);

    public Task Handle(SaleModifiedEvent notification, CancellationToken cancellationToken) =>
        Log(nameof(SaleModifiedEvent), notification);

    public Task Handle(SaleCancelledEvent notification, CancellationToken cancellationToken) =>
        Log(nameof(SaleCancelledEvent), notification);

    public Task Handle(ItemCancelledEvent notification, CancellationToken cancellationToken) =>
        Log(nameof(ItemCancelledEvent), notification);

    private Task Log(string eventName, INotification notification)
    {
        _logger.LogInformation("Event {EventName} published: {@Event}", eventName, notification);
        return Task.CompletedTask;
    }
}
