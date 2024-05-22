using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Challenges;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Challenges;

public class ChallengesService : IChallengesService
{
    private readonly IGeneralRepository<Challenge> _challengeRepository;
    private readonly IGeneralRepository<PatientChallenge> _patientChallengeRepository;


    public ChallengesService(
        IGeneralRepository<Challenge> challengeRepository, 
        IGeneralRepository<PatientChallenge> patientChallengeRepository)
    {
        _challengeRepository = challengeRepository;
        _patientChallengeRepository = patientChallengeRepository;
    }

    public Task<Challenge> GetLastChallengeInQueue()
    {
        var challenge = _challengeRepository.All()
            .OrderByDescending(c => c.EndDate)
            .Take(1)
            .FindAsync();
        
        return challenge;
    }

    public async Task<Challenge> GetById(int id)
    {
        return await _challengeRepository.FindAsync(id);
    }

    public async Task<string> GetTitleById(int id)
    {
        return await _challengeRepository.All()
            .ById(id)
            .Select(x => x.Title)
            .FindAsync();
    }

    public async Task<PatientChallenge> GetPatientChallenge(int patientId, int challengeId)
    {
        var result = await _patientChallengeRepository
            .All()
            .Include(x => x.Challenge)
            .FindAsync(pc => pc.PatientId == patientId && pc.ChallengeId == challengeId);

        return result;
    }
}