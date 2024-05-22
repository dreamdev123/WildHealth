using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Domain.Enums.Recommendations;

namespace WildHealth.Application.Commands.Documents;

public class UploadDocumentSourceCommand : IRequest
{
    
    public string Name { get; }
    public IFormFile? File { get;  }
    
    public int DocumentSourceTypeId { get; }
    
    public int[]? PersonaIds { get; }
    
    public HealthCategoryTag[]? Tags { get; }
    
    public string? Url { get; }
    
    public UploadDocumentSourceCommand(string name, int documentSourceTypeId, int[]? personaIds, HealthCategoryTag[]? tags, IFormFile? file, string? url)
    {
        Name = name;
        File = file;
        DocumentSourceTypeId = documentSourceTypeId;
        PersonaIds = personaIds;
        Tags = tags;
        Url = url;
    }
}