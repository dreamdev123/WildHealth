using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Patients;

public class UploadFileChangeStaffCommand : IRequest, IValidatabe
{
    public IFormFile File { get; }
        
    public UploadFileChangeStaffCommand(
        IFormFile file)
    {
        File = file;
    }
        
    #region validation
        
    private class Validator: AbstractValidator<UploadFileChangeStaffCommand>
    {
        public Validator()
        {
            RuleFor(x => x.File).NotNull();
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
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}