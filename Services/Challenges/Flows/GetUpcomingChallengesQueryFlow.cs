using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Challenges;
using WildHealth.Domain.Entities.Challenges;

namespace WildHealth.Application.Services.Challenges.Flows;

public record GetUpcomingChallengesQueryFlow(IQueryable<Challenge> Source, DateTime CurrentDate, int? Count) : IQueryFlow<ChallengeModel>
{
    public IQueryable<ChallengeModel> Execute()
    {
        IQueryable<Challenge> query = Source
            .Where(x => x.StartDate.Date > CurrentDate.Date)
            .OrderBy(x => x.StartDate);

        if (Count is > 0)
            query = query.Take(Count.Value);
        
        return query
            .Select(ch => new ChallengeModel
            {
                Id = ch.Id!.Value,
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