namespace WildHealth.Application.Extensions.Questionnaire
{
    public static class QuestionKey
    {
        public static string Height => "DEMOGRAPHICS_AND_BIOMETRICS_HEIGHT";
        public static string Weight => "DEMOGRAPHICS_AND_BIOMETRICS_WEIGHT";
        public static string Waist => "DEMOGRAPHICS_AND_BIOMETRICS_WAIST";
        public static string SystolicBloodPressure => "DEMOGRAPHICS_AND_BIOMETRICS_SBP_TOP";
        public static string DiastolicBloodPressure => "DEMOGRAPHICS_AND_BIOMETRICS_SBP_BOTTOM";
        public static string CancerScreening => "CANCER_SCREENING";
        public static string StartSleep => "FALL_ASLEEP_NIGHT";
        public static string StopSleep => "WAKE_UP";
        public static string RestedAfterSleep => "RESTED_READY_WAKEUP";
        public static string EnergyAfterSleep => "ENERGY_THROUGHOUT_THE_DAY";
        public static string Fasting => "PRACTICE_FEEDING";
        public static string Ethnicity => "DEMOGRAPHICS_AND_BIOMETRICS_ETHNICITY";
        public static string DiabetesStatus => "MEDICAL_HISTORY_DIABET";
        public static string DiabetesType => "MEDICAL_HISTORY_DIABET_TYPE";
        public static string SmokingStatus => "SMOKING_HISTORY";
        public static string RegularMeditation => "PERFORM_YOUR_ROUTINES";
        public static string PrimaryExercises => "PRIMARY_EXERCISE_GOAL";
        public static string SpecificDiet => "SPECIFIC_KIND_OF_DIET";
        public static string MedicalConditions => "MEDICAL_HISTORY_CONDITIONS";
        public static string FamilyHeartAttack => "MEDICAL_HISTORY_CONDITIONS_FAMILY";
        public static string WeeksExercise => "WEEKS_EXERCISE";
        public static string PharmacyName => "PREFERRED_PHARMACY_NAME";
        public static string PharmacyPhone => "PREFERRED_PHARMACY_PHONE";
        public static string PharmacyAddress => "PREFERRED_PHARMACY_ADDRESS";
        public static string PharmacyCity => "PREFERRED_PHARMACY_CITY";
        public static string PharmacyZipCode => "PREFERRED_PHARMACY_ZIPCODE";
        public static string PharmacyState => "PREFERRED_PHARMACY_STATE";
        public static string PharmacyCountry => "PREFERRED_PHARMACY_COUNTRY";
        public static string Phq21 => "PHQ_2_1";
        public static string Phq22 => "PHQ_2_2";
        
        public const string Medications = "MEDICATIONS";
        public const string Supplement = "SUPPLEMENT";
        public const string Allergies = "ALLERGIES";

    }
}
