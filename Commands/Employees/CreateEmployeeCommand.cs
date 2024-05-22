#nullable enable
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Commands.Employees
{
    public class CreateEmployeeCommand : IRequest<Employee>, IValidatabe
    {
        public string FirstName { get; }
        
        public string LastName { get; }
        
        public string Email { get; }

        public Gender Gender { get; }

        public int RoleId { get; }

        public int[] Permissions { get; }

        public int[] LocationIds { get; }
        
        public int PracticeId { get; }

        public string Credentials { get; }
        
        public bool RegisterInSchedulerSystem { get; }
        
        public string? Npi { get; }
        
        public string? RxntUserName { get; set; }
        
        public string? RxntPassword { get; set; }

        public CreateEmployeeCommand(
            string firstName, 
            string lastName, 
            string email,
            Gender gender,
            int roleId, 
            int[] permissions, 
            int[] locationIds,
            int practiceId,
            string credentials,
            bool registerInSchedulerSystem,
            string? npi = null,
            string? rxntUserName = null,
            string? rxntPassword = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Gender = gender;
            RoleId = roleId;
            Permissions = permissions;
            LocationIds = locationIds;
            PracticeId = practiceId;
            Credentials = credentials;
            RegisterInSchedulerSystem = registerInSchedulerSystem;
            Npi = npi;
            RxntUserName = rxntUserName;
            RxntPassword = rxntPassword;
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

        private class Validator : AbstractValidator<CreateEmployeeCommand>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotEmpty();
                RuleFor(x => x.LastName).NotEmpty();
                RuleFor(x => x.Email).NotEmpty().EmailAddress();
                RuleFor(x => x.RoleId).GreaterThan(0);
                RuleFor(x => x.PracticeId).GreaterThan(0);
            }
        }

        #endregion
    }
}