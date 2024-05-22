using System;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Employees;

public class GetEmployeePhotoUrlCommand : IRequest<string?>
{
    public int UserId { get; set; }

    public GetEmployeePhotoUrlCommand(int userId)
    {
        UserId = userId;
    }

    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetEmployeePhotoUrlCommand>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
        }
    }

    #endregion
}
