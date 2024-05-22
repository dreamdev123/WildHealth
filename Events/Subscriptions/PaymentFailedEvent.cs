using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models._Base;
using WildHealth.Integration.Models.Invoices;
using FluentValidation;
using MediatR;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.Events.Subscriptions;

public class PaymentFailedEvent : INotification, IValidatable
{
    public PaymentFailedEvent(InvoiceIntegrationModel invoice, IntegrationVendor vendor)
    {
        Invoice = invoice;
        Vendor = vendor;
    }

    public InvoiceIntegrationModel Invoice { get; }
    
    public IntegrationVendor Vendor { get; }
    
    #region validation

    private class Validator : AbstractValidator<PaymentFailedEvent>
    {
        public Validator()
        {
            RuleFor(x => x.Invoice).NotNull();
            RuleFor(x => x.Vendor).IsInEnum();
        }
    }

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}