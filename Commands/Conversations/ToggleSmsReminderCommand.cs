using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models._Base;
using FluentValidation;
using MediatR;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Commands.Conversations;

public class ToggleSmsReminderCommand: IRequest<UserSetting>, IValidatable
{
    public ToggleSmsReminderCommand(int patientId, bool isActive)
    {
        PatientId = patientId;
        IsActive = isActive;
    }

    public int PatientId { get; }
    
    public bool IsActive { get; }
    
    #region validation

    private class Validator : AbstractValidator<ToggleSmsReminderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
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