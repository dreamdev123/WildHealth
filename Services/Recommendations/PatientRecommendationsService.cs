using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Recommendations;

public class PatientRecommendationsService: IPatientRecommendationsService
{
    private readonly IGeneralRepository<PatientRecommendation> _patientRecommendationsRepository;

    public PatientRecommendationsService(IGeneralRepository<PatientRecommendation> patientRecommendationsRepository)
    {
        _patientRecommendationsRepository = patientRecommendationsRepository;
    }

    public async Task<PatientRecommendation[]> GetByPatientIdAsync(int patientId)
    {
        var result = await _patientRecommendationsRepository
            .All()
            .Where(o => o.PatientId == patientId)
            .Include(o => o.Patient)
            .ThenInclude(o => o.User)
            .ToArrayAsync();
        
        return result;
    }

    public async Task<PatientRecommendation[]> GetByPatientIdAndTagAsync(int patientId, HealthCategoryTag tag)
    {
        var result = await _patientRecommendationsRepository
            .All()
            .Where(o => o.PatientId == patientId)
            .Where(o => o.Recommendation.Tags.Select(x => x.Tag).Contains(tag))
            .Include(pr => pr.Recommendation)
            .ThenInclude(r => r.Tags)
            .ToArrayAsync();

        return result;
    }

    public async Task<PatientRecommendation[]> GetAddOnReportRecommendations(int patientId, HealthCategoryTag tag)
    {
        var result = await _patientRecommendationsRepository
            .All()
            .Where(o => o.PatientId == patientId)
            .Where(o => o.Recommendation.Tags.Select(x => x.Tag).Contains(tag) && o.Recommendation.Tags.Select(x => x.Tag).Contains(HealthCategoryTag.AddOnReport))
            .Include(pr => pr.Recommendation)
            .ThenInclude(r => r.Tags)
            .ToArrayAsync();

        return result;
    }
    
    public async Task<PatientRecommendation> GetByIdAsync(int id)
    {
        var result = await _patientRecommendationsRepository
            .All()
            .ById(id)
            .Include(pr => pr.Recommendation)
            .ThenInclude(r => r.Tags)
            .Include(pr => pr.Patient)
            .ThenInclude(p => p.User)
            .FindAsync();
        
        return result;
    }

    public async Task<PatientRecommendation> CreateAsync(PatientRecommendation patientRecommendation)
    {
        await _patientRecommendationsRepository.AddAsync(patientRecommendation);

        await _patientRecommendationsRepository.SaveAsync();
        
        return patientRecommendation;
    }
    
    public async Task DeleteAsync(PatientRecommendation patientRecommendation)
    {
        _patientRecommendationsRepository.Delete(patientRecommendation);

        await _patientRecommendationsRepository.SaveAsync();
    }

    public async Task<PatientRecommendation[]> GetByIdsAsync(int[] ids)
    {
        var results = await _patientRecommendationsRepository
            .All()
            .Where(x => ids.Any(id => id == x.Id!.Value))
            .Include(x => x.Recommendation)
            .ThenInclude(r => r.Tags)
            .ToArrayAsync();

        return results;
    }

    public async Task<PatientRecommendation[]> GetUnverifiedByPatientIdAsync(int patientId, VerificationMethod verificationMethod)
    {
        var results = await  _patientRecommendationsRepository
            .All()
            .Include(pr => pr.Recommendation)
            .ThenInclude(r => r.VerificationMethods)
            .Where(pr => pr.PatientId == patientId 
                && pr.Recommendation.VerificationMethods.Any(o => o.VerificationMethod == verificationMethod)
                && pr.Verified == false)
            .ToArrayAsync();

        return results;
    }
}