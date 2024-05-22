using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.Commands.SyncRecords;

public class SendDorothyChargeDeniedCommsCommand : IRequest, IValidatabe
{
    public string EncounterId { get; }
    public IntegrationVendor Vendor { get; }

    public SendDorothyChargeDeniedCommsCommand(string encounterId, IntegrationVendor vendor)
    {
        EncounterId = encounterId;
        Vendor = vendor;
    }
    
    #region validation

    private class Validator : AbstractValidator<SendDorothyChargeDeniedCommsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EncounterId).NotNull().NotEmpty();
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