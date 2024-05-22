using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Reports._Base;
using WildHealth.Common.Models.Reports;
using WildHealth.Domain.Entities.Reports;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.HealthReports
{
    public class EditHealthReportCommand : IRequest<HealthReport>, IValidatabe
    {
        public int Id { get; }
        public int PatientId { get; }
        public ReportRecommendationBaseModel MacronutrientRecommendation { get; }
        public ReportRecommendationBaseModel MethylationRecommendation { get; }
        public ReportRecommendationBaseModel CompleteDietRecommendation { get; }
        public ReportRecommendationBaseModel KryptoniteFoodsRecommendation { get; }
        public ReportRecommendationBaseModel SuperFoodsRecommendation { get; }
        public ReportRecommendationBaseModel VitaminsAndMicronutrientsRecommendation { get; }
        public ReportRecommendationBaseModel ExerciseAndRecoveryRecommendation { get; }
        public ReportRecommendationBaseModel SleepRecommendation { get; }
        public ReportRecommendationBaseModel MicrobiomeRecommendation { get; }
        public ReportRecommendationBaseModel NeurobehavioralRecommendation { get; }
        public ReportRecommendationBaseModel DementiaRecommendation { get; }
        public ReportRecommendationBaseModel InflammationRecommendation { get; }
        public ReportRecommendationBaseModel CardiovascularRecommendation { get; }
        public ReportRecommendationBaseModel InsulinResistanceRecommendation { get; }
        public ReportRecommendationBaseModel LongevityRecommendation { get; }
        public ReportRecommendationBaseModel SupplementsRecommendation { get; }
        public ICollection<ReportRecommendationModel> Recommendations { get; }
        
        public EditHealthReportCommand(
            int id,
            int patientId,
            ReportRecommendationBaseModel macronutrientRecommendation, 
            ReportRecommendationBaseModel methylationRecommendation, 
            ReportRecommendationBaseModel completeDietRecommendation, 
            ReportRecommendationBaseModel kryptoniteFoodsRecommendation, 
            ReportRecommendationBaseModel superFoodsRecommendation, 
            ReportRecommendationBaseModel vitaminsAndMicronutrientsRecommendation, 
            ReportRecommendationBaseModel exerciseAndRecoveryRecommendation, 
            ReportRecommendationBaseModel sleepRecommendation, 
            ReportRecommendationBaseModel microbiomeRecommendation, 
            ReportRecommendationBaseModel neurobehavioralRecommendation, 
            ReportRecommendationBaseModel dementiaRecommendation, 
            ReportRecommendationBaseModel inflammationRecommendation, 
            ReportRecommendationBaseModel cardiovascularRecommendation, 
            ReportRecommendationBaseModel insulinResistanceRecommendation, 
            ReportRecommendationBaseModel longevityRecommendation, 
            ReportRecommendationBaseModel supplementsRecommendation, 
            ICollection<ReportRecommendationModel> recommendations)
        {
            Id = id;
            PatientId = patientId;
            MacronutrientRecommendation = macronutrientRecommendation;
            MethylationRecommendation = methylationRecommendation;
            CompleteDietRecommendation = completeDietRecommendation;
            KryptoniteFoodsRecommendation = kryptoniteFoodsRecommendation;
            SuperFoodsRecommendation = superFoodsRecommendation;
            VitaminsAndMicronutrientsRecommendation = vitaminsAndMicronutrientsRecommendation;
            ExerciseAndRecoveryRecommendation = exerciseAndRecoveryRecommendation;
            SleepRecommendation = sleepRecommendation;
            MicrobiomeRecommendation = microbiomeRecommendation;
            NeurobehavioralRecommendation = neurobehavioralRecommendation;
            DementiaRecommendation = dementiaRecommendation;
            InflammationRecommendation = inflammationRecommendation;
            CardiovascularRecommendation = cardiovascularRecommendation;
            InsulinResistanceRecommendation = insulinResistanceRecommendation;
            LongevityRecommendation = longevityRecommendation;
            SupplementsRecommendation = supplementsRecommendation;
            Recommendations = recommendations;
        }

        #region validation

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);


        private class Validator : AbstractValidator<EditHealthReportCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.Recommendations).NotNull();
            }
        }

        #endregion
    }
}