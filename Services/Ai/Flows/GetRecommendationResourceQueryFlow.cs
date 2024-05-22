using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Ai;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Recommendations;

namespace WildHealth.Application.Services.Ai.Flows;

public record GetRecommendationResourceQueryFlow(IQueryable<PatientRecommendation> Source, Guid UniversalId) : IQueryFlow<AiResourceModel>
{
    public IQueryable<AiResourceModel> Execute()
    {
        return Source
            .Where(x => x.UniversalId == UniversalId)
            .Include(x => x.Recommendation)
            .Select(pr => new AiResourceModel
            {
                IsPatientDocument = true,
                Document = pr.Content,
                DocumentType = AiConstants.ResourceTypes.PatientRecommendation,
                UniversalId = pr.UniversalId.ToString()
            });
    }
}