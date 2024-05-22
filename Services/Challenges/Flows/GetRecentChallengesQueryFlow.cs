using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Challenges;
using WildHealth.Domain.Entities.Challenges;

namespace WildHealth.Application.Services.Challenges.Flows;

public record GetRecentChallengesQueryFlow(IQueryable<Challenge> Source, int PatientId, DateTime CurrentDate, int? Count) : IQueryFlow<ChallengeModel>
{
    public IQueryable<ChallengeModel> Execute()
    {
        IQueryable<Challenge> query = Source
            .Where(x => x.EndDate < CurrentDate)
            .OrderByDescending(x => x.EndDate);

        if (Count.HasValue)
        {
            query = query.Take(Count.Value);
        }
        
        return query
            .Select(ch => new ChallengeModel
            {
                Id = ch.GetId(),
                IsParticipant = ch.PatientChallenges.Any(x => x.PatientId == PatientId && x.IsParticipant),
                IsCompleted = ch.PatientChallenges.Any(x => x.PatientId == PatientId && x.CompletedAt.HasValue),
                IsLiked = ch.PatientChallenges.Any(x => x.PatientId == PatientId && x.Liked),
                Completed = ch.Completed,
                Description = ch.Description,
                Likes = ch.Likes,
                Participants = ch.Participants,
                Title = ch.Title,
                ImageName = ch.ImageName,
                EndDate = ch.EndDate,
                StartDate = ch.StartDate
            });
    }
}