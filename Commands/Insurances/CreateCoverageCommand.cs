using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Insurance;
using WildHealth.Domain.Entities.Insurances;
using FluentValidation;
using MediatR;
using WildHealth.Common.Models.Common;

namespace WildHealth.Application.Commands.Insurances
{
    public class CreateCoverageCommand : IRequest<Coverage>, IValidatabe
    {
        public int? UserId { get; }
        
        public int? PatientId { get; }
                
        public int InsuranceId { get; }

        public string MemberId { get; }
        
        public bool IsPrimary { get; }
        
        public AttachmentModel[]? Attachments { get; }
        
        public PolicyHolderModel? PolicyHolder { get; }
        
        protected CreateCoverageCommand(
            int? userId,
            int? patientId,
            int insuranceId,
            string memberId, 
            bool isPrimary,
            PolicyHolderModel? policyHolder,
            AttachmentModel[]? attachments)
        {
            UserId = userId;
            PatientId = patientId;
            InsuranceId = insuranceId;
            MemberId = memberId;
            IsPrimary = isPrimary;
            PolicyHolder = policyHolder;
            Attachments = attachments ?? Array.Empty<AttachmentModel>();
        }
        
        public static CreateCoverageCommand ByUser(
            int userId,
            int insuranceId,
            string memberId, 
            bool isPrimary,
            PolicyHolderModel? policyHolder,
            AttachmentModel[]? attachments)
        {
            return new CreateCoverageCommand(
                userId: userId,
                patientId: null,
                insuranceId: insuranceId,
                memberId: memberId,
                isPrimary: isPrimary,
                policyHolder: policyHolder,
                attachments: attachments
            );
        }
        
        public static CreateCoverageCommand OnBehalf(
            int patientId,
            int insuranceId,
            string memberId, 
            bool isPrimary,
            PolicyHolderModel? policyHolder,
            AttachmentModel[]? attachments)
        {
            return new CreateCoverageCommand(
                userId: null,
                patientId: patientId,
                insuranceId: insuranceId,
                memberId: memberId,
                isPrimary: isPrimary,
                policyHolder: policyHolder,
                attachments: attachments
            );
        }

        #region validation

        private class Validator : AbstractValidator<CreateCoverageCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);
                RuleFor(x => x.PatientId).GreaterThan(0).When(x => x.PatientId.HasValue);
                RuleFor(x => x.InsuranceId).GreaterThan(0);
                RuleFor(x => x.MemberId).NotNull().NotEmpty();
                RuleFor(x => x.PolicyHolder).SetValidator(new PolicyHolderModel.Validator());
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