using System.Threading.Tasks;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Enums.Recommendations;

namespace WildHealth.Application.Services.Recommendations;

public interface IPatientRecommendationsService
{
    Task<PatientRecommendation> CreateAsync(PatientRecommendation patientRecommendation);

    Task DeleteAsync(PatientRecommendation patientRecommendation);

    Task<PatientRecommendation[]> GetByPatientIdAsync(int patientId);

    Task<PatientRecommendation[]> GetByPatientIdAndTagAsync(int patientId, HealthCategoryTag tag);

    Task<PatientRecommendation[]> GetAddOnReportRecommendations(int patientId, HealthCategoryTag tag);

    Task<PatientRecommendation[]> GetByIdsAsync(int[] ids);

    Task<PatientRecommendation> GetByIdAsync(int id);

    Task<PatientRecommendation[]> GetUnverifiedByPatientIdAsync(int patientId, VerificationMethod verificationMethod);
}