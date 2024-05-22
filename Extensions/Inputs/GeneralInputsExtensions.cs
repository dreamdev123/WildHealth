using WildHealth.Common.Models.Inputs;
using WildHealth.Domain.Entities.Inputs;

namespace WildHealth.Application.Extensions.Inputs
{
    public static class GeneralInputsExtensions
    {
        /// <summary>
        /// Copies values from model to entity
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="model"></param>
        public static void Update(this GeneralInputs inputs, GeneralInputsModel model)
        {
            inputs.DietChoice = model.DietChoice;
            inputs.ExerciseGoal = model.ExerciseGoal;
            inputs.ExerciseActivitiesFrequency = model.ExerciseActivitiesFrequency;
            inputs.MeditationFrequency = model.MeditationFrequency;
            inputs.Sleep = model.Sleep;
            inputs.DeepSleep = model.DeepSleep;
            inputs.Rem = model.Rem;
            inputs.RealAge = model.RealAge;
            inputs.BiologicalAge = model.BiologicalAge;
            inputs.FastingRegularly = model.FastingRegularly;
            inputs.CancerScreeningCompleted = model.CancerScreeningCompleted;
            inputs.BrainOptimization = model.BrainOptimization;
            inputs.MindOptimization = model.MindOptimization;
            inputs.BodyOptimization = model.BodyOptimization;
            inputs.GutOptimization = model.GutOptimization;
            inputs.LongevityOptimization = model.LongevityOptimization;
            inputs.BioHackingOptimization = model.BioHackingOptimization;
            inputs.Ethnicity = model.Ethnicity;
            inputs.SmokingCategory = model.SmokingCategory;
            inputs.DiabetesType = model.DiabetesType;
            inputs.FamilyHeartAttack = model.FamilyHeartAttack;
            inputs.ChronicKidneyDisease = model.ChronicKidneyDisease;
            inputs.AtrialFibrillation = model.AtrialFibrillation;
            inputs.BloodPressureTreatment = model.BloodPressureTreatment;
            inputs.RheumatoidArthritis = model.RheumatoidArthritis;
            inputs.Mesa = model.Mesa;
        }
    }
}
