using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Insurances;

public class RemitFilesService : IRemitFilesService
{
    private readonly IGeneralRepository<RemitFile> _remitFilesRepository;

    public RemitFilesService(IGeneralRepository<RemitFile> remitFilesRepository)
    {
        _remitFilesRepository = remitFilesRepository;
    }
    
    public async Task<RemitFile> CreateAsync(RemitFile remitFile)
    {
        await _remitFilesRepository.AddAsync(remitFile);
        await _remitFilesRepository.SaveAsync();

        return remitFile;
    }
    
    public async Task<RemitFile> GetByIdAsync(int id)
    {
        var remitFile = await _remitFilesRepository
            .All()
            .Where(o => o.Id == id)
            .Include(o => o.Remits)
                .ThenInclude(o => o.RemitServicePayments)
                .ThenInclude(o => o.RemitAdjustments)
            .Include(o => o.Remits)
                .ThenInclude(o => o.Claim)
                .ThenInclude(o => o.ClaimantSyncRecord)
            .FirstOrDefaultAsync();
        
        if (remitFile is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Unable to locate a [RemitFile] with [Id] = {id}");
        }

        return remitFile;
    }

    public async Task<RemitFile?> GetByFileNameAsync(string fileName)
    {
        var remitFile = await _remitFilesRepository
            .All()
            .FirstOrDefaultAsync(o => o.FileName == fileName);

        return remitFile;
    }
}