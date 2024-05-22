using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class ReceiveLabOrderCommand : IRequest<LabOrder>, IValidatabe
    {
        public int PatientId { get; }
        
        public int ReportId { get; }
        
        public string OrderNumber { get; }
        
        public string[] TestCodes { get; }
        
        public ReceiveLabOrderCommand(
            int patientId, 
            int reportId, 
            string orderNumber, 
            string[] testCodes)
        {
            PatientId = patientId;
            ReportId = reportId;
            OrderNumber = orderNumber;
            TestCodes = testCodes;
        }

        #region validation

        private class Validator : AbstractValidator<ReceiveLabOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.ReportId).GreaterThan(0);
                RuleFor(x => x.OrderNumber).NotNull().NotEmpty();
                RuleFor(x => x.TestCodes).NotNull().NotEmpty();
                RuleForEach(x => x.TestCodes).NotNull().NotEmpty();
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