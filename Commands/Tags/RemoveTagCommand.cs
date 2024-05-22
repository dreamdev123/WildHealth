using WildHealth.Application.Commands._Base;
using WildHealth.Common.Extensions;
using WildHealth.Domain.Entities.Patients;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Tags;

public class RemoveTagCommand : IRequest, IValidatabe
{
    public Patient Patient { get; }
    public string Tag { get; }

    public RemoveTagCommand(Patient patient, string tag)
    {
        Patient = patient;
        Tag = tag;
    }
    
    private class Validator : AbstractValidator<RemoveTagCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Patient).NotNull();
            RuleFor(x => x.Tag).NotNull().NotEmpty().NotWhitespace();
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
}