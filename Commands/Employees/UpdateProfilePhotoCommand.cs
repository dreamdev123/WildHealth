using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Employees;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Employees;

public class UpdateProfilePhotoCommand : IRequest, IValidatabe
{
    public Employee Employee { get; }

    public IFormFile File { get; }
    
    public UpdateProfilePhotoCommand(Employee employee, IFormFile file)
    {
        Employee = employee;
        File = file;
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

    private class Validator : AbstractValidator<UpdateProfilePhotoCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Employee).NotNull();
            RuleFor(x => x.File).NotNull();
        }
    }

    #endregion
}