using System.Threading.Tasks;
using WildHealth.Domain.Entities.Challenges;

namespace WildHealth.Application.Services.Challenges;

public interface IChallengesService
{
    Task<Challenge> GetLastChallengeInQueue();
    Task<Challenge> GetById(int id);
    Task<string> GetTitleById(int id);
    Task<PatientChallenge> GetPatientChallenge(int patientId, int challengeId);
}