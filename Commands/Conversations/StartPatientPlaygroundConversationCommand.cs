using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Conversations;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Conversations;

public class StartPatientPlaygroundConversationCommand : IRequest<Conversation>, IValidatabe
{
    public int EmployeeId { get; }
    
    public int PatientId { get; }

    public int LocationId { get; }

    public int PracticeId { get; }
    
    public string Prompt { get; }

    public StartPatientPlaygroundConversationCommand(
        int employeeId,
        int patientId,
        int locationId,
        int practiceId, 
        string prompt)
    {
        EmployeeId = employeeId;
        PatientId = patientId;
        LocationId = locationId;
        PracticeId = practiceId;
        Prompt = prompt;
    }

    #region validation

    private class Validator : AbstractValidator<StartPatientPlaygroundConversationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).GreaterThan(0);
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.PracticeId).GreaterThan(0);
            RuleFor(x => x.LocationId).GreaterThan(0);
            RuleFor(x => x.Prompt).NotNull().NotEmpty();
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