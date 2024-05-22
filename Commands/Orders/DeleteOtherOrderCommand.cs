using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders;

public class DeleteOtherOrderCommand : IRequest<OtherOrder>, IValidatabe
{
    public int Id { get;}
 
    public DeleteOtherOrderCommand(int id)
    {
        Id = id;
    }
    
    #region validation

    private class Validator : AbstractValidator<DeleteOtherOrderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
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