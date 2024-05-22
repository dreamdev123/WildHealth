using System;
using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Actions;
using WildHealth.Domain.Enums.Actions;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Domain.Actions;

public class CreateCallToActionCommand : IRequest<CallToAction>, IValidatabe
{
    public int PatientId { get; }
    
    public ActionType Type { get; }
    
    public DateTime? ExpiresAt { get; }
    
    public ActionReactionType[] Reactions { get; }
    
    public IDictionary<string, string> Data { get; }
    
    public CreateCallToActionCommand(
        int patientId, 
        ActionType type, 
        DateTime? expiresAt, 
        ActionReactionType[] reactions, 
        IDictionary<string, string> data)
    {
        PatientId = patientId;
        Type = type;
        ExpiresAt = expiresAt;
        Reactions = reactions;
        Data = data;
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
    
    private class Validator : AbstractValidator<CreateCallToActionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.ExpiresAt)
                .GreaterThan(DateTime.UtcNow)
                .When(x => x.ExpiresAt.HasValue);

            RuleFor(x => x.Reactions).NotNull().NotEmpty();
        }
    }

    #endregion
}