using FluentValidation;
using MediatR;
using WildHealth.Common.Models._Base;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Commands;

public class NotifyAboutPreauthorizeRequestCommand : IRequest, IValidatable
{
    public int[] Ids { get; }
    
    public NotifyAboutPreauthorizeRequestCommand(int[] ids)
    {
        Ids = ids;
    }


    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<NotifyAboutPreauthorizeRequestCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Ids).NotNull().NotEmpty();
            RuleForEach(x => x.Ids).GreaterThan(0);
        }
    }

    #endregion
}