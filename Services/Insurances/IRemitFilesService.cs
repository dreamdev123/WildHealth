using System.Threading.Tasks;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Services.Insurances;

public interface IRemitFilesService
{
    /// <summary>
    /// Get remit file by file name
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task<RemitFile?> GetByFileNameAsync(string fileName);

    /// <summary>
    /// Get remit file by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<RemitFile> GetByIdAsync(int id);

    /// <summary>
    /// Create remit file
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    Task<RemitFile> CreateAsync(RemitFile claim);
}