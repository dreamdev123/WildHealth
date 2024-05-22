#nullable enable
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Commands.Employees
{
    public class UpdateEmployeeCommand : IRequest<Employee>, IValidatabe
    {
        public int Id { get; }
        
        public string FirstName { get; }
        
        public string LastName { get; }

        public string Email { get; }

        public Gender Gender { get; }
        
        public int RoleId { get; }

        public int[] Permissions { get; }

        public int[] LocationIds { get; }
        
        public int PracticeId { get; }

        public string SchedulerAccountId { get; }

        public string Credentials { get; }
        
        public string? Npi { get; }
        
        public string? RxntUserName { get; set; }
        
        public string? RxntPassword { get; set; }

        public UpdateEmployeeCommand(
            int id,
            string firstName, 
            string lastName, 
            string schedulerAccountId,
            int roleId, 
            int[] permissions,
            int[] locationIds, 
            int practiceId,
            string email,
            Gender gender,
            string credentials,
            string? npi = null,
            string? rxntUserName = null,
            string? rxntPassword = null)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            SchedulerAccountId = schedulerAccountId;
            LocationIds = locationIds;
            RoleId = roleId;
            Permissions = permissions;
            PracticeId = practiceId;
            Email = email;
            Gender = gender;
            Credentials = credentials;
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

        private class Validator : AbstractValidator<UpdateEmployeeCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
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