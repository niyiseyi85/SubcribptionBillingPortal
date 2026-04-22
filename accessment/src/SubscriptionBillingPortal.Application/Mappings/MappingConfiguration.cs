using Mapster;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Entities;

namespace SubscriptionBillingPortal.Application.Mappings;

/// <summary>
/// Central Mapster mapping configuration — registered once at startup.
/// Maps domain entities to DTOs. Never exposes domain models to external layers.
/// </summary>
public static class MappingConfiguration
{
    public static void Configure()
    {
        TypeAdapterConfig<Customer, CustomerDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .Map(dest => dest.FullName, src => src.FullName)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        TypeAdapterConfig<Subscription, SubscriptionDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.CustomerId, src => src.CustomerId)
            .Map(dest => dest.PlanType, src => src.Plan.PlanType.ToString())
            .Map(dest => dest.BillingInterval, src => src.Plan.BillingInterval.ToString())
            .Map(dest => dest.PlanPrice, src => src.Plan.Price.Amount)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.ActivatedAt, src => src.ActivatedAt)
            .Map(dest => dest.CancelledAt, src => src.CancelledAt);

        TypeAdapterConfig<Invoice, InvoiceDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.SubscriptionId, src => src.SubscriptionId)
            .Map(dest => dest.Amount, src => src.Amount.Amount)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.IssuedAt, src => src.IssuedAt)
            .Map(dest => dest.PaidAt, src => src.PaidAt)
            .Map(dest => dest.PaymentReference, src => src.PaymentReference);
    }
}
