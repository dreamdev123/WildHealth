using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Insurances;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Insurances
{
    public class GetCoveragesCommand : IRequest<Coverage[]>, IValidatabe
    {
        public int? UserId { get; }
        
        public int? PatientId { get; }

        public GetCoveragesCommand(
            int? userId = null,
            int? patientId = null)
        {
            UserId = userId;
            PatientId = patientId;
        }

        #region validation

        private class Validator : AbstractValidator<GetCoveragesCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);
                RuleFor(x => x.PatientId).GreaterThan(0).When(x => x.PatientId.HasValue);
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
