using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Challenges;

public record LikeChallengeCommand(  
    int PatientId,
    int ChallengeId,
    Guid UniversalId) : IRequest<Unit>, IValidatabe
{
    public bool IsValid() => new Validator().Validate(this).IsValid;
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<LikeChallengeCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.ChallengeId).GreaterThan(0);
        }
    }
}