using WildHealth.Application.Commands._Base;
using FluentValidation;
using WildHealth.Domain.Entities.Orders;
using MediatR;
using WildHealth.Domain.Entities.EmployerProducts;

namespace WildHealth.Application.Commands.Orders
{
    public class CreateEpigeneticOrderCommand : IRequest<EpigeneticOrder>, IValidatabe
    {
        public int PatientId { get; }
        
        public int[] AddOnIds { get; }
        
        public bool ProcessPayment { get; }
        
        public EmployerProduct? EmployerProduct { get; }
        
        public CreateEpigeneticOrderCommand(
            int patientId, 
            int[] addOnIds, 
            bool processPayment,
            EmployerProduct? employerProduct)
        {
            PatientId = patientId;
            AddOnIds = addOnIds;
            ProcessPayment = processPayment;
            EmployerProduct = employerProduct;
        }
        
        #region Validation
        
        private class Validator : AbstractValidator<CreateEpigeneticOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.AddOnIds).NotNull().NotEmpty();
                RuleForEach(x => x.AddOnIds).GreaterThan(0);
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