using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Enums.AddOns;

namespace WildHealth.Application.Commands.AddOns;

public class UpdateAddOnsFromFileCommand : IRequest, IValidatabe
{
    public IFormFile AddOnsFile { get; }
    public AddOnProvider Provider { get; }
    
    public UpdateAddOnsFromFileCommand(IFormFile addOnsFile, AddOnProvider provider)
    {
        AddOnsFile = addOnsFile;
        Provider = provider;
    }

    #region Validation
    
    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new UpdateAddOnsFromFileCommand.Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    /// <returns></returns>
    public void Validate() => new UpdateAddOnsFromFileCommand.Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<UpdateAddOnsFromFileCommand>
    {
        public Validator()
        {
            RuleFor(x => x.AddOnsFile).NotNull();
        }
    }
    
    #endregion
}