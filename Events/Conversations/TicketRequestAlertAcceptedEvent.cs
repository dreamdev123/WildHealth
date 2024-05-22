using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Events.Conversations;

public class TicketRequestAlertAcceptedEvent : INotification, IValidatabe
{
    public int PatientId { get; }
    
    public int LocationId { get; }
    
    public int PracticeId { get; }
    
    public string Subject { get; }
    
    public TicketRequestAlertAcceptedEvent(
        int patientId, 
        int locationId, 
        int practiceId, 
        string subject)
    {
        PatientId = patientId;
        LocationId = locationId;
        PracticeId = practiceId;
        Subject = subject;
    }

    #region validation

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<TicketRequestAlertAcceptedEvent>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.PracticeId).GreaterThan(0);
            RuleFor(x => x.LocationId).GreaterThan(0);
            RuleFor(x => x.Subject).NotNull().NotEmpty();
        }
    }

    #endregion
}