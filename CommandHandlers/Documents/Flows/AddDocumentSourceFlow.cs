using System;
using System.Linq;
using System.Net;
using WildHealth.Application.Events.Documents;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Entities.Files.Blobs;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Documents.Flows;

public class AddDocumentSourceFlow : IMaterialisableFlow
{
    private readonly string _name;
    private readonly DocumentSourceType _documentSourceType;
    private readonly BlobFile? _file;
    private readonly int[] _personaIds;
    private readonly HealthCategoryTag[] _tags;
    private readonly HealthCategoryTag[] _recommendedTags;
    private readonly string? _url;
    private readonly int? _patientId;

    public AddDocumentSourceFlow(
        string name, 
        DocumentSourceType documentSourceType,
        int[]? personaIds,
        HealthCategoryTag[] recommendedTags,
        HealthCategoryTag[]? tags,
        BlobFile? file,
        string? url,
        int? patientId = null)
    {
        _name = name;
        _documentSourceType = documentSourceType;
        _file = file;
        _recommendedTags = recommendedTags;
        _tags = tags ?? _recommendedTags;
        _personaIds = Array.Empty<int>();
        _url = url;
        _patientId = patientId;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_file is null && _url is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "File or url is required for knowledge base document upload.");
        }

        var result = MaterialisableFlowResult.Empty;
        
        var documentSource = new DocumentSource(_name, _documentSourceType, _file, _url);

        result += documentSource.Added();
        
        if (_patientId.HasValue)
        {
            result += new PatientDocumentSource()
            {
                PatientId = _patientId.Value,
                DocumentSource = documentSource
            }.Added();
        }
        
        return result
               + new DocumentSourceCreatedEvent(documentSource)
               + _tags.Select(tag => new DocumentSourceTag { Tag = tag, DocumentSource = documentSource }.Added())
               + _personaIds.Select(id => new DocumentSourcePersona { PersonaId = id, DocumentSource = documentSource }.Added());
    }
}