using FluentValidation;
using MediatR;
using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Commands.Users
{
    public class CreateInitialUserCommand : IRequest<User>, IValidatabe
    {
        public string FirstName { get; }
        public string LastName { get; }
        public string Email { get; }
        public string? PhoneNumber { get; }
        public DateTime Birthday { get; }
        public Gender Gender { get; }
        public int PracticeId { get; }
        public UserType UserType { get; }

        public CreateInitialUserCommand(
            string firstName,
            string lastName,
            string email,
            string? phoneNumber,
            DateTime birthday,
            Gender gender,
            int practiceId,
            UserType userType
        )
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Birthday = birthday;
            Gender = gender;
            PracticeId = practiceId;
            UserType = userType;
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<CreateInitialUserCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.FirstName).NotNull().NotEmpty();
                RuleFor(x => x.LastName).NotNull().NotEmpty();
                RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
                RuleFor(x => x.Birthday).NotEmpty();
            }
        }
    }
}
