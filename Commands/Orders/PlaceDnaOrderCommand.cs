using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Orders
{
    public class PlaceDnaOrderCommand : IRequest, IValidatabe
    {
        public int OrderId;
        public int PatientId;

        public PlaceDnaOrderCommand(int orderId, int patientId)
        {
            OrderId = orderId;
            PatientId = patientId;
        }

        #region private

        private class Validator : AbstractValidator<PlaceDnaOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.OrderId).GreaterThan(0);
                RuleFor(x => x.PatientId).GreaterThan(0);
            }
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}