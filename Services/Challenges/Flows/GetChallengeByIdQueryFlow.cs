using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Challenges;
using WildHealth.Domain.Entities.Challenges;
using WildHealth.Shared.Data.Queries;

namespace WildHealth.Application.Services.Challenges.Flows;

public record GetChallengeByIdQueryFlow
    (IQueryable<Challenge> Source, int ChallengeId, int PatientId) : IQueryFlow<ChallengeModel>
{
    public IQueryable<ChallengeModel> Execute()
    {
        return Source
            .ById(ChallengeId)
            .Select(ch => new ChallengeModel
            {
                Id = ch.Id!.Value,
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
