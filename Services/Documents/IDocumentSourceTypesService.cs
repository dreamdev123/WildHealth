using System.Threading.Tasks;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Enums.Documents;

namespace WildHealth.Application.Services.Documents;

public interface IDocumentSourceTypesService
{
    Task<DocumentSourceType> GetByIdAsync(int id);

     Task<DocumentSourceType[]> GetAsync();

     Task<DocumentSourceType> GetByAutomatedDocumentSourceType(AutomatedDocumentSourceType type);
}