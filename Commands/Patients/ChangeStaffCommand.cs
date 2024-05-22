using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Patients;

public record ChangeStaffCommand(
    string FromHealthCoachEmail, 
    string ToHealthCoachEmail, 
    string FromProviderEmail, 
    string ToProviderEmail, 
    string PatientEmail, 
    bool AttemptToRescheduleIndividualAppointments,
    bool AttemptToRescheduleInDifferentTimeSlot,
    bool ShouldSendChangeMessageToPatient) : IRequest, IValidatabe
{
    #region validation

    private class Validator : AbstractValidator<ChangeStaffCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientEmail).NotNull().NotEmpty();
            RuleFor(x => x.ToHealthCoachEmail).NotNull().When(o => !string.IsNullOrEmpty(o.FromHealthCoachEmail));
            RuleFor(x => x.ToProviderEmail).NotNull().When(o => !string.IsNullOrEmpty(o.FromProviderEmail));
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