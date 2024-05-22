using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.SyncRecords;

public class OptOutOfDorothySubscriptionCommand : IRequest, IValidatabe
{
    public int SyncRecordId { get; }

    public OptOutOfDorothySubscriptionCommand(int syncRecordId)
    {
        SyncRecordId = syncRecordId;
    }
    
    #region validation

    private class Validator : AbstractValidator<OptOutOfDorothySubscriptionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.SyncRecordId).GreaterThan(0);
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