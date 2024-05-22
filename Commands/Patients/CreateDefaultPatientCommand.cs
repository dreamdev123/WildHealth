using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Patients
{
    public class CreateDefaultPatientCommand : IRequest<Patient>, IValidatabe
    {
        public Practice Practice { get; }

        public Location Location { get; }
        
        public Employee Employee { get; }
        
        public IDictionary<string, string> DataTemplates { get; }

        public CreateDefaultPatientCommand(
            Practice practice, 
            Location location, 
            Employee employee,
            IDictionary<string, string> dataTemplates)
        {
            Practice = practice;
            Location = location;
            Employee = employee;
            DataTemplates = dataTemplates;
        }
        
        #region validation

        private class Validator : AbstractValidator<CreateDefaultPatientCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Location).NotNull();
                RuleFor(x => x.Practice).NotNull();
                RuleFor(x => x.Employee).NotNull();
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
}