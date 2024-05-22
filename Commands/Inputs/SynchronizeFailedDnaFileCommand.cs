using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Inputs;

public class SynchronizeFailedDnaFileCommand : IRequest, IValidatabe
{
    public string FileName { get; }

    public SynchronizeFailedDnaFileCommand(string fileName)
    {
        FileName = fileName;
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

    private class Validator : AbstractValidator<SynchronizeFailedDnaFileCommand>
    {
        public Validator()
        {
            RuleFor(x => x.FileName).NotNull().NotEmpty();
        }
    }

    #endregion
}