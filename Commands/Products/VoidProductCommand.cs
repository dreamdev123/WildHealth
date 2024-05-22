using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Products;

public class VoidProductCommand : IRequest<PatientProduct>, IValidatabe
{
    public int Id { get; }
    
    public VoidProductCommand(int id)
    {
        Id = id;
    }
    
    #region validation

    private class Validator : AbstractValidator<VoidProductCommand>
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