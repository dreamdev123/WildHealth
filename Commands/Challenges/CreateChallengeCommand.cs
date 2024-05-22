using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Challenges;

public record CreateChallengeCommand(
    string Title, 
    string Description, 
    int DurationInDays, 
    IFormFile Image) : IRequest<Unit>, IValidatabe
{
    public bool IsValid() => new Validator().Validate(this).IsValid;
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<CreateChallengeCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(128);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(1024);
            RuleFor(x => x.Image).NotNull();
            RuleFor(x => x.Image.FileName).MaximumLength(32);
        }
    }
}