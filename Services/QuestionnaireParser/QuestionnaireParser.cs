using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Extensions.Questionnaire;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Enums;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Domain.Models.Questionnaires;
using Newtonsoft.Json;

namespace WildHealth.Application.Services.QuestionnaireParser
{
    /// <summary>
    /// <see cref="IQuestionnaireParser"/>
    /// </summary>
    public class QuestionnaireParser : IQuestionnaireParser
    {
        private static readonly Dictionary<string, string> CheckQuestionsMaps = new Dictionary<string, string>
        {
            { nameof(GeneralInputs.BloodPressureTreatment), "High blood pressure"},
            { nameof(GeneralInputs.ChronicKidneyDisease), "Chronic Kidney Disease"},
            { nameof(GeneralInputs.AtrialFibrillation), "Atrial Fibrillation"},
            { nameof(GeneralInputs.RheumatoidArthritis), "Rheumatoid Arthritis"},
            { nameof(GeneralInputs.FamilyHeartAttack), "Heart Attack in 1st degree relative < 60 years of age"},
        };

        /// <summary>
        /// <see cref="IQuestionnaireParser.Parse(GeneralInputs, QuestionnaireResult)"/>
        /// </summary>
        /// <param name="generalInputs"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public GeneralInputs Parse(GeneralInputs generalInputs, QuestionnaireResult result)
        {
            var questionnaireResultDomain = QuestionnaireResultDomain.Create(result);

            if (IsDefault(generalInputs.Sleep))
            {
                generalInputs.Sleep = GetSleep(questionnaireResultDomain.GetResult(QuestionKey.StartSleep), questionnaireResultDomain.GetResult(QuestionKey.StopSleep));
            }

            if (IsDefault(generalInputs.DeepSleep))
            {
                generalInputs.DeepSleep = GetDecimal(questionnaireResultDomain.GetResult(QuestionKey.RestedAfterSleep)) /4;
            }

            if (IsDefault(generalInputs.Rem))
            {
                generalInputs.Rem = GetDecimal(questionnaireResultDomain.GetResult(QuestionKey.EnergyAfterSleep)) / 4;
            }

            if (IsDefault(generalInputs.CancerScreeningCompleted))
            {
                generalInputs.CancerScreeningCompleted = QuestionnaireAnswers.GetValue<YesNo>(questionnaireResultDomain.GetResult(QuestionKey.CancerScreening));
            }

            if (IsDefault(generalInputs.FastingRegularly))
            {
                generalInputs.FastingRegularly = QuestionnaireAnswers.GetValue<YesNo>(questionnaireResultDomain.GetResult(QuestionKey.Fasting));
            }

            if (IsDefault(generalInputs.Ethnicity))
            {
                generalInputs.Ethnicity = QuestionnaireAnswers.GetValue<Ethnicity>(questionnaireResultDomain.GetResult(QuestionKey.Ethnicity));
            }

            if (IsDefault(generalInputs.DiabetesType))
            {
                generalInputs.DiabetesType = GetDiabetesType(result);
            }

            if (IsDefault(generalInputs.SmokingCategory))
            {
                generalInputs.SmokingCategory = QuestionnaireAnswers.GetValue<SmokingCategory>(questionnaireResultDomain.GetResult(QuestionKey.SmokingStatus));
            }

            if (IsDefault(generalInputs.ExerciseActivitiesFrequency))
            {
                generalInputs.ExerciseActivitiesFrequency = GetExerciseActivitiesFrequency(result);
            }

            if (IsDefault(generalInputs.MeditationFrequency))
            {
                generalInputs.MeditationFrequency = GetIntDigit(questionnaireResultDomain.GetResult(QuestionKey.RegularMeditation));
            }

            if (IsDefault(generalInputs.ExerciseGoal))
            {
                generalInputs.ExerciseGoal = QuestionnaireAnswers.GetValue<ExerciseType>(questionnaireResultDomain.GetResult(QuestionKey.PrimaryExercises));
            }

            if (IsDefault(generalInputs.DietChoice))
            {
                generalInputs.DietChoice = QuestionnaireAnswers.GetValue<DietType>(questionnaireResultDomain.GetResult(QuestionKey.SpecificDiet));
            }

            if (IsDefault(generalInputs.FamilyHeartAttack))
            {
                generalInputs.FamilyHeartAttack = GetFromCheckMany(questionnaireResultDomain.GetResult(QuestionKey.FamilyHeartAttack), nameof(GeneralInputs.FamilyHeartAttack));
            }

            if (IsDefault(generalInputs.BloodPressureTreatment))
            {
                generalInputs.BloodPressureTreatment = GetFromCheckMany(questionnaireResultDomain.GetResult(QuestionKey.MedicalConditions), nameof(GeneralInputs.ChronicKidneyDisease));
            }

            if (IsDefault(generalInputs.ChronicKidneyDisease))
            {
                generalInputs.ChronicKidneyDisease = GetFromCheckMany(questionnaireResultDomain.GetResult(QuestionKey.MedicalConditions), nameof(GeneralInputs.ChronicKidneyDisease));
            }

            if (IsDefault(generalInputs.AtrialFibrillation))
            {
                generalInputs.AtrialFibrillation = GetFromCheckMany(questionnaireResultDomain.GetResult(QuestionKey.MedicalConditions), nameof(GeneralInputs.AtrialFibrillation));
            }

            if (IsDefault(generalInputs.RheumatoidArthritis))
            {
                generalInputs.RheumatoidArthritis = GetFromCheckMany(questionnaireResultDomain.GetResult(QuestionKey.MedicalConditions), nameof(GeneralInputs.RheumatoidArthritis));
            }

            return generalInputs;
        }

        #region private

        private DiabetesType GetDiabetesType(QuestionnaireResult result)
        {
            var questionnaireResultDomain = QuestionnaireResultDomain.Create(result);

            var status = QuestionnaireAnswers.GetValue<YesNo>(questionnaireResultDomain.GetResult(QuestionKey.DiabetesStatus));

            if (status == YesNo.No)
            {
                return DiabetesType.No;
            }

            return QuestionnaireAnswers.GetValue<DiabetesType>(questionnaireResultDomain.GetResult(QuestionKey.DiabetesType));
        }

        private int GetExerciseActivitiesFrequency(QuestionnaireResult result)
        {
            var questionnaireResultDomain = QuestionnaireResultDomain.Create(result);

            return Math.Min(7,
                GetIntDigit(questionnaireResultDomain.GetResult(QuestionKey.WeeksExercise)) ?? 0 
            );
        }

        private decimal? GetSleep(string start, string stop)
        {
            var isStartSleepCorrect = TimeSpan.TryParse(start, out var startSleep);
            var isStopSleepCorrect = TimeSpan.TryParse(stop, out var stopSleep);

            if (!isStartSleepCorrect || !isStopSleepCorrect)
            {
                return null;
            }

            var sleep = stopSleep.Subtract(startSleep).TotalHours;
            if (stopSleep < startSleep)
            {
                sleep += 24;
            }
            
            return new decimal(sleep);
        }

        #endregion

        #region Helpers

        private bool IsDefault(int? value) => !value.HasValue;

        private bool IsDefault(decimal? value) => !value.HasValue;

        private bool IsDefault<T>(T value) where T : Enum => QuestionnaireAnswers.IsDefault(value);

        private YesNo GetYesNo(IEnumerable<string> results, Dictionary<string, string> map, string key)
        {
            return results.Contains(map.TryGetValue(key, out var bpt) ? bpt : null) ? YesNo.Yes : YesNo.No;
        }

        private YesNo GetFromCheckMany(string value, string key)
        {
            if (string.IsNullOrEmpty(value))
            {
                return QuestionnaireAnswers.GetDefault<YesNo>();
            }

            var results = JsonConvert.DeserializeObject<CheckManyQuestionResultModel>(value)?.V!;

            return GetYesNo(results, CheckQuestionsMaps, key);
        }

        private int? GetInt(string value)
        {
            return int.TryParse(value, out var result) ? result  : new int?();
        }

        private decimal? GetDecimal(string value)
        {
            return decimal.TryParse(value, out var result) ? result : new decimal?();
        }

        private int? GetIntDigit(string value)
        {
            return string.IsNullOrEmpty(value) ? new int?() : GetInt(new string(value.Where(char.IsDigit).ToArray()));
        }

        #endregion
    }
}