using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Documents;

public class DocumentSourcesService : IDocumentSourcesService
{
    private readonly IGeneralRepository<DocumentSource> _documentSourceRepository;

    public DocumentSourcesService(IGeneralRepository<DocumentSource> documentSourceRepository)
    {
        _documentSourceRepository = documentSourceRepository;
    }

    public async Task<DocumentSource> GetByIdAsync(int id)
    {
        var result = await _documentSourceRepository
            .All()
            .ById(id)
            .Include(x => x.Integrations)
            .ThenInclude(x => x.Integration)
            .Include(ds => ds.DocumentSourceType)
            .Include(ds => ds.Tags)
            .Include(ds => ds.DocumentSourcePersonas)
            .ThenInclude(dsp => dsp.Persona)
            .Include(ds => ds.DocumentChunks)
            .Include(ds => ds.PatientDocumentSources)
            .ThenInclude(pds => pds.Patient)
            .ThenInclude(p => p.User)
            .FindAsync();

        return result;
    }

    public async Task<DocumentSource[]> GetAsync(int[] documentSourceTypeIds)
    {
        var result = await _documentSourceRepository
            .All()
            .Where(o => documentSourceTypeIds.Contains(o.DocumentSourceTypeId))
            .Include(ds => ds.DocumentSourceType)
            .Include(ds => ds.DocumentChunks)
            .Include(ds => ds.Tags)
            .ToArrayAsync();

        return result;
    }

    public async Task<DocumentSource?> GetByIntegrationIdAsync(
        string integrationId,
        IntegrationVendor vendor,
        string purpose)
    {
        var result = await _documentSourceRepository
            .All()
            .ByIntegrationId<DocumentSource, DocumentSourceIntegration>(integrationId, vendor, purpose)
            .Include(x => x.Integrations)
            .ThenInclude(x => x.Integration)
            .Include(ds => ds.DocumentSourceType)
            .Include(ds => ds.Tags)
            .Include(ds => ds.DocumentSourcePersonas)
            .ThenInclude(dsp => dsp.Persona)
            .Include(ds => ds.DocumentChunks)
            .Include(ds => ds.PatientDocumentSources)
            .ThenInclude(pds => pds.Patient)
            .ThenInclude(p => p.User)
            .FindAsync();

        return result;
    }

    public async Task<DocumentSource> StoreChunks(DocumentSource documentSource, DocumentChunk[] chunks)
    {
        documentSource.DocumentChunks = chunks;
        
        _documentSourceRepository.Edit(documentSource);

        await _documentSourceRepository.SaveAsync();
        
        return documentSource;
    }
    
    public async Task<DocumentSource> CreateAsync(DocumentSource documentSource)
    {
        await _documentSourceRepository.AddAsync(documentSource);

        await _documentSourceRepository.SaveAsync();

        return documentSource;
    }

    public async Task<DocumentSource> UpdateAsync(DocumentSource documentSource)
    {
        _documentSourceRepository.Edit(documentSource);

        await _documentSourceRepository.SaveAsync();

        return documentSource;
    }

    public async Task<DocumentSource?> GetByNameAndTypeAsync(string name, int documentSourceTypeId)
    {
        var result = await _documentSourceRepository
            .All()
            .Include(ds => ds.DocumentSourceType)
            .Include(ds => ds.DocumentChunks)
            .Where(ds => ds.Name.ToLower() == name.ToLower() && ds.DocumentSourceTypeId == documentSourceTypeId)
            .FirstOrDefaultAsync();
        
        return result;
    }
}