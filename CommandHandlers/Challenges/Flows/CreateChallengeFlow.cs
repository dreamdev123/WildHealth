using System;
using OneOf.Monads;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Extensions;
using WildHealth.Domain.Entities.Challenges;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.CommandHandlers.Challenges.Flows;

public record CreateChallengeFlow(Option<Challenge> LastChallenge,
    string ImageName,
    string Title,
    string Description,
    int DurationInDays,
    DateTime UtcNow) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var startDate = !LastChallenge.HasValue() || LastChallenge.Value().EndDate.Date < UtcNow.Date ? 
            UtcNow.Date : LastChallenge.Value().EndDate.Date + TimeSpan.FromDays(1);

        var endDate = DurationInDays > 0 ? 
            startDate + TimeSpan.FromDays(DurationInDays - 1) : startDate.WeekEndDate(); 
        
        return new Challenge
        {
            Title = Title,
            Description = Description,
            StartDate = startDate,
            EndDate = endDate,
            ImageName = ImageName
        }.Added();
    }
}