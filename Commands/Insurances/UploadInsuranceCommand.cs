using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Common;
using WildHealth.Domain.Entities.Attachments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Insurances
{
    public class UploadInsuranceCommand : IRequest<Attachment[]>, IValidatabe
    {
        public int? UserId { get; }
        
        public int? PatientId { get; }
        
        public string CoverageId { get; }
        
        public AttachmentModel[] Attachments { get; }

        protected UploadInsuranceCommand(
            int? userId, 
            int? patientId,
            string coverageId,
            AttachmentModel[]? attachments)
        {
            UserId = userId;
            PatientId = patientId;
            CoverageId = coverageId;
            Attachments = attachments ?? Array.Empty<AttachmentModel>();
        }

        public static UploadInsuranceCommand ByUser(
            int userId,
            string coverageId,
            AttachmentModel[]? attachments)
        {
            return new UploadInsuranceCommand(
                userId: userId,
                patientId: null,
                coverageId: coverageId,
                attachments: attachments
            );
        }
        
        public static UploadInsuranceCommand OnBehalf(
            int patientId,
            string coverageId,
            AttachmentModel[]? attachments)
        {
            return new UploadInsuranceCommand(
                userId: null,
                patientId: patientId,
                coverageId: coverageId,
                attachments: attachments
            );
        }
        
        #region validation

        private class Validator : AbstractValidator<UploadInsuranceCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);
                RuleFor(x => x.PatientId).GreaterThan(0).When(x => x.PatientId.HasValue);
                RuleFor(x => x.CoverageId).NotNull().NotEmpty();
                RuleFor(x => x.Attachments).NotNull().NotEmpty();
                RuleForEach(x => x.Attachments).NotNull();
                RuleForEach(x => x.Attachments).SetValidator(new AttachmentModel.Validator());
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