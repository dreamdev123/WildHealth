using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Documents;

public class DocumentSourceTypesService : IDocumentSourceTypesService
{
    private readonly IGeneralRepository<DocumentSourceType> _documentSourceTypeRepository;
    private readonly int _documentSourceTypeAutomatedPodcast = 7;
    
    public DocumentSourceTypesService(IGeneralRepository<DocumentSourceType> documentSourceTypeRepository)
    {
        _documentSourceTypeRepository = documentSourceTypeRepository;
    }

    public async Task<DocumentSourceType> GetByIdAsync(int id)
    {
        var result = await _documentSourceTypeRepository
            .All()
            .ById(id)
            .FindAsync();

        return result;
    }

    public async Task<DocumentSourceType[]> GetAsync() 
    {
        var result = await _documentSourceTypeRepository
            .All()
            .ToArrayAsync();

        return result;
    }

    public async Task<DocumentSourceType> GetByAutomatedDocumentSourceType(AutomatedDocumentSourceType type)
    {
        return type switch
        {
            AutomatedDocumentSourceType.Podcast => await GetByIdAsync(_documentSourceTypeAutomatedPodcast),
            _ => throw new AppException(HttpStatusCode.NotFound,
                $"Definition not defined for [AutomatedDocumentSourceType]: {type.ToString()}")
        };
    }
}