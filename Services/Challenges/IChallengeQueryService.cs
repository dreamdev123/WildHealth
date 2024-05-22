using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Challenges;

namespace WildHealth.Application.Services.Challenges;

public interface IChallengeQueryService
{
    Task<List<ChallengeModel>> GetActiveChallenges(DateTime asOfDate, int patientId, bool includeImageUrl = false);
    string? GetFileImageUrlByName(string name);
    Task<List<ChallengeModel>> GetUpcomingChallenges(DateTime utcNow, int? count);
    Task<List<ChallengeModel>> GetRecentChallenges(DateTime currentDate, int patientId, int? count, bool includeImageUrl = false);
    Task<ChallengeModel> GetById(int challengeId, int patientId, bool includeImageUrl = false);
}