using System;
using System.Threading.Tasks;
using WildHealth.Common.Enums.Patients;
using WildHealth.Redash.Clients.Models;

namespace WildHealth.Application.Services.Redash;

public interface IRedashService
{
    /// <summary>
    /// Gets fhir charge submissions
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<QueryResultModel<FhirChargeSubmissionModel>> QueryFhirChargeSubmissionsAsync(DateTime startDate, DateTime endDate, int practiceId);

    /// <summary>
    /// Gets fhir charge denials
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<QueryResultModel<FhirChargeDenialModel>> QueryFhirChargeDenialsAsync(DateTime startDate, DateTime endDate, int practiceId);
    
    /// <summary>
    /// Gets the NPS score information
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    Task<QueryResultModel<PatientNpsScoreModel>> QueryNpsScoresAsync(int patientId);
}