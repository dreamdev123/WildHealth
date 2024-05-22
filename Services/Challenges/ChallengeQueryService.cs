using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Challenges.Flows;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Challenges;
using WildHealth.Domain.Entities.Challenges;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Challenges;

public class ChallengeQueryService : IChallengeQueryService
{
    private readonly IGeneralRepository<Challenge> _challengeRepository;
    private readonly IAzureBlobService _azureBlobService;
    
    public ChallengeQueryService(
        IGeneralRepository<Challenge> challengeRepository, 
        IAzureBlobService azureBlobService)
    {
        _challengeRepository = challengeRepository;
        _azureBlobService = azureBlobService;
    }

    public async Task<List<ChallengeModel>> GetActiveChallenges(DateTime asOfDate, int patientId, bool includeImageUrl = false)
    {
        var challenges = await _challengeRepository
            .All()
            .Query(source => new GetActiveChallengesQueryFlow(source, asOfDate, patientId))
            .ToListAsync();

        if (includeImageUrl)
        {
            foreach (var challenge in challenges)
            {
                challenge.ImageUrl = GetFileImageUrlByName(challenge.ImageName);
            }
        }
            
        return challenges;
    }
    
    public async Task<ChallengeModel> GetById(int challengeId, int patientId, bool includeImageUrl = false)
    {
        var challenge = await _challengeRepository
            .All()
            .Query(source => new GetChallengeByIdQueryFlow(source, challengeId, patientId))
            .FindAsync();

        if (includeImageUrl)
        {

            challenge.ImageUrl = GetFileImageUrlByName(challenge.ImageName);
        }
            
        return challenge;
    }
    
    public async Task<List<ChallengeModel>> GetRecentChallenges(DateTime currentDate, int patientId, int? count, bool includeImageUrl = false)
    {
        var challenges = await _challengeRepository
            .All()
            .Query(source => new GetRecentChallengesQueryFlow(source, patientId, currentDate, count))
            .ToListAsync();
            
        if (includeImageUrl)
        {
            foreach (var challenge in challenges)
            {
                challenge.ImageUrl = GetFileImageUrlByName(challenge.ImageName);
            }
        }
            
        return challenges;
    }

    public string? GetFileImageUrlByName(string name)
    {
        try
        {
            var uri = _azureBlobService.GetBlobSasUri(AzureBlobContainers.Media, name, 1);

            return uri.ToString();
        }
        catch { /* Failed to grab image from storage */ }

        return null;
    }

    public async Task<List<ChallengeModel>> GetUpcomingChallenges(DateTime utcNow, int? count)
    {
        var challenges = await _challengeRepository
            .All()
            .Query(source => new GetUpcomingChallengesQueryFlow(source, utcNow, count))
            .ToListAsync();
            
        return challenges;
    }
}