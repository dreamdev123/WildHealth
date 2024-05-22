using WildHealth.Domain.Entities.Locations;
using MediatR;
using WildHealth.Application.Commands._Base;
using FluentValidation;

namespace WildHealth.Application.Commands.Locations
{
    public class UpdateLocationCommand : IRequest<Location>, IValidatabe
    {
        public int Id { get; }

        public string Name { get; }

        public string Description { get; }

        public string Country { get; }

        public string City { get; }

        public string State { get; }

        public string ZipCode { get; }

        public string StreetAddress1 { get;  }

        public string StreetAddress2 { get; }

        public int? CareCoordinatorId { get; }

        public UpdateLocationCommand(
            int id,
            string name,
            string description,
            string country,
            string city,
            string state,
            string zipCode,
            string streetAddress1,
            string streetAddress2,
            int? careCoordinatorId)
        {
            Id = id;
            Name = name;
            Description = description;
            Country = country;
            City = city;
            State = state;
            ZipCode = zipCode;
            StreetAddress1 = streetAddress1;
            StreetAddress2 = streetAddress2;
            CareCoordinatorId = careCoordinatorId;
        }

        #region private 
        
        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<UpdateLocationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);

                RuleFor(x => x.Name)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(50);

                RuleFor(x => x.Description)
                    .NotNull()
                    .MaximumLength(250);

                RuleFor(x => x.Country)
                  .NotNull()
                  .NotEmpty()
                  .MaximumLength(50);

                RuleFor(x => x.City)
                  .NotNull()
                  .NotEmpty()
                  .MaximumLength(50);

                RuleFor(x => x.State)
                  .NotNull()
                  .NotEmpty()
                  .MaximumLength(50);

                RuleFor(x => x.ZipCode)
                  .NotNull()
                  .NotEmpty()
                  .MaximumLength(10);

                RuleFor(x => x.StreetAddress1)
                  .NotNull()
                  .NotEmpty()
                  .MaximumLength(100);

                RuleFor(x => x.StreetAddress2)
                  .NotNull()
                  .MaximumLength(100);
            }
        }
        
        #endregion
    }
}