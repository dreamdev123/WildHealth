using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.MdmCodes;
using WildHealth.Domain.Enums.Notes;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.MdmCodes
{
    public  class MdmCodesService: IMdmCodesService
    {
        private readonly IGeneralRepository<MdmCode> _mdmCodesRepository;

        public MdmCodesService(IGeneralRepository<MdmCode> mdmCodesRepository)
        {
            _mdmCodesRepository = mdmCodesRepository;
        }

        public async Task<MdmCode> GetByIdAsync(int id)
        {
            var mdmCode = await _mdmCodesRepository
                .All()
                .ById(id)
                .FirstOrDefaultAsync();

            if (mdmCode is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Mdm code with id = {id} does not exist.");
            }

            return mdmCode;
        }

        public async Task<IEnumerable<MdmCode>> GetAll(NoteType? noteType)
        {
            var mdmCodes = await _mdmCodesRepository
                .All()
                .Include(x => x.Categories)
                .ThenInclude(x => x.Items)
                .ToListAsync();

            return noteType.HasValue
                ? mdmCodes.Where(x => x.NoteTypes is null || x.NoteTypes.Contains(noteType.Value))
                : mdmCodes;
        }
    }
}
