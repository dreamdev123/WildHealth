using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Commands;

public class DeletePreauthorizeRequestCommand : IRequest<PreauthorizeRequest>, IValidatabe
{
    public int Id { get; }
    
    public DeletePreauthorizeRequestCommand(int id)
    {
        Id = id;
    }

    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<DeletePreauthorizeRequestCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    #endregion
}