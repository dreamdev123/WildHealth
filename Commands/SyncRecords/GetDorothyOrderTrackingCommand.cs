using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.ShippingOrders;

namespace WildHealth.Application.Commands.SyncRecords;

public class GetDorothyOrderTrackingCommand : IRequest<TrackingModel>, IValidatabe
{
    public Guid ClaimUniversalId { get; }

    public GetDorothyOrderTrackingCommand(Guid claimUniversalId)
    {
        ClaimUniversalId = claimUniversalId;
    }
    
    #region validation

    private class Validator : AbstractValidator<GetDorothyOrderTrackingCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ClaimUniversalId).NotNull();
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