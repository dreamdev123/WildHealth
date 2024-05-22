using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.MdmCodes;
using WildHealth.Domain.Enums.Notes;

namespace WildHealth.Application.Services.MdmCodes
{
    public interface IMdmCodesService
    {
        Task<MdmCode> GetByIdAsync(int id);
        
        Task<IEnumerable<MdmCode>> GetAll(NoteType? noteType);
    }
}
