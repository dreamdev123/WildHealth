using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Ai;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Documents;

namespace WildHealth.Application.Services.Ai.Flows;

public record GetDocumentChunkResourceQueryFlow(IQueryable<DocumentChunk> Source, Guid UniversalId) : IQueryFlow<AiResourceModel>
{
    public IQueryable<AiResourceModel> Execute()
    {
        return Source
            .Where(x => x.UniversalId == UniversalId)
            .Select(dc => new AiResourceModel
            {
                IsPatientDocument = false,
                Document = dc.Content,
                DocumentType = AiConstants.ResourceTypes.GeneralKnowledgeBaseChunk,
                UniversalId = dc.UniversalId.ToString()
            });
    }
}