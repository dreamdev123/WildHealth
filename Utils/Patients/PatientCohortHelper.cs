using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MassTransit.Futures.Contracts;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Enums.Patients;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Utils.Patients;

public class PatientCohortHelper : IPatientCohortHelper
{
    private readonly IPatientsService _patientsService;

    public PatientCohortHelper(
        IPatientsService patientsService)
    {
        _patientsService = patientsService;
    }

    /// <summary>
    /// Get a list of all cohorts that a patient belongs to
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    public async Task<PatientCohort[]> GetCohortsForPatientId(int patientId)
    {
        var cohortResult = await _patientsService.GetPatientCohort(patientId);

        var resultType = cohortResult.GetType();

        var cohorts = new List<PatientCohort>();
        
        foreach (PatientCohort val in (PatientCohort[]) Enum.GetValues(typeof(PatientCohort)))
        {
            var property = resultType.GetProperty(val.ToString());

            var cohortValue = (bool?)property?.GetValue(cohortResult) ?? false;

            if (cohortValue)
            {
                cohorts.Add(val);
            }
        }

        return cohorts.ToArray();
    }

    /// <summary>
    /// Returns result of whether the patient is in the given cohorts, logical AND
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="cohorts"></param>
    /// <returns></returns>
    public async Task<bool> PatientInAllCohorts(int patientId, PatientCohort[] cohorts)
    {
        var patientCohorts = await GetCohortsForPatientId(patientId);

        return cohorts.All(o => patientCohorts.Contains(o));
    }

    /// <summary>
    /// Returns result of whether the patient is in the given cohorts, logical OR
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="cohorts"></param>
    /// <returns></returns>
    public async Task<bool> PatientInAnyCohorts(int patientId, PatientCohort[] cohorts)
    {
        var patientCohorts = await GetCohortsForPatientId(patientId);

        return cohorts.Any(o => patientCohorts.Contains(o));
    }
    
    #region convenience

    public async Task<bool> IsPremiumPatient(int patientId)
    {
        return await PatientInAnyCohorts(patientId, new[]
        {
            PatientCohort.TagPremium, 
            PatientCohort.TagPremiumPeak,
            PatientCohort.TagCoreVip,
            PatientCohort.TagCoreSuperVip,
            PatientCohort.TagAthletics,
            PatientCohort.TagAthleticsVip,
            PatientCohort.TagPremiumHalf,
            PatientCohort.TagPremiumVip
        });
    }

    #endregion
}