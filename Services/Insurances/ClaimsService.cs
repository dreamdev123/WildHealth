using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Insurances;

public class ClaimsService : IClaimsService
{
    private readonly IGeneralRepository<Claim> _claimsRepository;

    public ClaimsService(IGeneralRepository<Claim> claimsRepository)
    {
        _claimsRepository = claimsRepository;
    }

    /// <summary>
    /// Create claim
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    public async Task<Claim> CreateAsync(Claim claim)
    {
        await _claimsRepository.AddAsync(claim);
        await _claimsRepository.SaveAsync();

        return claim;
    }

    /// <summary>
    /// Update claim
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    public async Task<Claim> UpdateAsync(Claim claim)
    {
        _claimsRepository.Edit(claim);

        await _claimsRepository.SaveAsync();

        return claim;
    }

    public async Task<Claim> GetById(int id)
    {
        var claim = await _claimsRepository
            .All()
            .Where(o => o.Id == id)
            .Include(o => o.ClaimantSyncRecord)
            .FirstOrDefaultAsync();

        if (claim is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Unable to locate a [Claim] with [Id] = {id}");
        }

        return claim;
    }

    public async Task<Claim?> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor, string purpose)
    {
        var result = await _claimsRepository
            .All()
            .ByIntegrationId<Claim, ClaimIntegration>(integrationId, vendor, purpose)
            .Include(o => o.ClaimantNote)
                .ThenInclude(o => o.Patient)
                .ThenInclude(o => o.User)
            .Include(o => o.ClaimantNote)
                .ThenInclude(o => o.Appointment)
                .ThenInclude(o => o.PatientProduct)
            .FirstOrDefaultAsync();
        
        return result;
    }
}