using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Inputs;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Inputs;

public class UpdateLabInputHighlightCommand : IRequest<LabInput>, IValidatabe
{
    public int Id { get; }
    
    public int PatientId { get; }
    
    public bool IsActive { get; }
    
    public UpdateLabInputHighlightCommand(int id, int patientId, bool isActive)
    {
        Id = id;
        PatientId = patientId;
        IsActive = isActive;
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

    private class Validator : AbstractValidator<UpdateLabInputHighlightCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    #endregion
}