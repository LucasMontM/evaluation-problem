using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Raised when a sale is created.
/// </summary>
public sealed record SaleCreatedEvent(Guid SaleId, string SaleNumber, Guid CustomerId, Guid BranchId, decimal TotalAmount, DateTime OccurredAt) : INotification;

/// <summary>
/// Raised when a sale is modified.
/// </summary>
public sealed record SaleModifiedEvent(Guid SaleId, string SaleNumber, decimal TotalAmount, DateTime OccurredAt) : INotification;

/// <summary>
/// Raised when a sale is cancelled.
/// </summary>
public sealed record SaleCancelledEvent(Guid SaleId, string SaleNumber, DateTime OccurredAt) : INotification;

/// <summary>
/// Raised when a single item of a sale is cancelled.
/// </summary>
public sealed record ItemCancelledEvent(Guid SaleId, string SaleNumber, Guid ItemId, Guid ProductId, decimal SaleTotalAmount, DateTime OccurredAt) : INotification;
