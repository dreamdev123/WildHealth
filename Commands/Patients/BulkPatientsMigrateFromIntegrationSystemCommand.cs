using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Enums.Patient;
using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Extensions;

namespace WildHealth.Application.Commands.Patients
{
    public class BulkPatientsMigrateFromIntegrationSystemCommand : IRequest<IEnumerable<BulkPatientsMigrateResultModel>>, IValidatabe
    {
        public int PracticeId { get; }

        public int LocationId { get; }
        
        public PatientOnBoardingStatus Status { get; }

        public IFormFile File { get; }

        public string PlanMapJson { get; }

        public bool SaveMode { get; }

        public bool SendConfirmationEmail { get; }

        public bool UsePlanFromIntegrationSystem { get; }
        
        public bool ConfirmAgreements { get; }

        public BulkPatientsMigrateFromIntegrationSystemCommand(
            IFormFile file, 
            int practiceId,
            int locationId,
            PatientOnBoardingStatus status,
            string planMapJson,
            bool saveMode,
            bool sendConfirmationEmail,
            bool usePlanFromIntegrationSystem, 
            bool confirmAgreements)
        {
            File = file;
            PracticeId = practiceId;
            LocationId = locationId;
            Status = status;
            PlanMapJson = planMapJson;
            SaveMode = saveMode;
            SendConfirmationEmail = sendConfirmationEmail;
            UsePlanFromIntegrationSystem = usePlanFromIntegrationSystem;
            ConfirmAgreements = confirmAgreements;
        }

        #region validation

        private class Validator : AbstractValidator<BulkPatientsMigrateFromIntegrationSystemCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.LocationId).GreaterThan(0);
                RuleFor(x => x.PlanMapJson).NotEmpty().NotWhitespace();
                RuleFor(x => x.File).NotNull();
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
