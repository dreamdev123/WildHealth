using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.Services.Documents;

public interface IDocumentSourcesService
{
    Task<DocumentSource> GetByIdAsync(int id);

    Task<DocumentSource[]> GetAsync(int[] documentSourceTypeIds);
    
    Task<DocumentSource?> GetByIntegrationIdAsync(
        string integrationId,
        IntegrationVendor vendor,
        string purpose);
    
    Task<DocumentSource> StoreChunks(DocumentSource source, DocumentChunk[] chunks);

    Task<DocumentSource> CreateAsync(DocumentSource documentSource);
    
    Task<DocumentSource> UpdateAsync(DocumentSource documentSource);

    Task<DocumentSource?> GetByNameAndTypeAsync(string name, int documentSourceTypeId);
}