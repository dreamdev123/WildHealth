using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Services.Insurances;

public interface IClaimStatusFilesService
{
    /// <summary>
    /// Get claim status file by file name
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task<ClaimStatusFile?> GetByFileNameAsync(string fileName);

    /// <summary>
    /// Get claim status file by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ClaimStatusFile> GetByIdAsync(int id);

    /// <summary>
    /// Create claim status file
    /// </summary>
    /// <param name="claimStatusFile"></param>
    /// <returns></returns>
    Task<ClaimStatusFile> CreateAsync(ClaimStatusFile claimStatusFile);
}