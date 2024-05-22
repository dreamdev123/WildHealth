using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders
{
    public class GetLabOrdersIframeCommand : IRequest<string>, IValidatabe
    {
        public int PatientId { get; }

        public GetLabOrdersIframeCommand(int patientId)
        {
            PatientId = patientId;
        }
        
        #region Validation
        
        private class Validator : AbstractValidator<GetLabOrdersIframeCommand>
        {
            public Validator()
            {
                RuleFor(c => c.PatientId).GreaterThan(0);
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