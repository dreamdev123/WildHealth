using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Insurances;

public class ClaimStatusFilesService : IClaimStatusFilesService
{
    private readonly IGeneralRepository<ClaimStatusFile> _claimStatusFilesRepository;

    public ClaimStatusFilesService(IGeneralRepository<ClaimStatusFile> claimStatusFilesRepository)
    {
        _claimStatusFilesRepository = claimStatusFilesRepository;
    }
    
    public async Task<ClaimStatusFile> CreateAsync(ClaimStatusFile claimStatusFile)
    {
        await _claimStatusFilesRepository.AddAsync(claimStatusFile);
        await _claimStatusFilesRepository.SaveAsync();

        return claimStatusFile;
    }
    
    public async Task<ClaimStatusFile> GetByIdAsync(int id)
    {
        var claimStatusFile = await _claimStatusFilesRepository
            .All()
            .Where(o => o.Id == id)
            .FirstOrDefaultAsync();
        
        if (claimStatusFile is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Unable to locate a [ClaimStatusFile] with [Id] = {id}");
        }

        return claimStatusFile;
    }

    public async Task<ClaimStatusFile?> GetByFileNameAsync(string fileName)
    {
        var claimStatusFile = await _claimStatusFilesRepository
            .All()
            .FirstOrDefaultAsync(o => o.FileName == fileName);

        return claimStatusFile;
    }
}