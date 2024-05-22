using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.DNAFiles;

namespace WildHealth.Application.Services.DnaFiles
{
    /// <summary>
    /// Provides methods for working with DNA files
    /// </summary>
    public interface IDnaFilesService
    {
        /// <summary>
        /// Returns all files from the blob storage
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<DNAFileModel>> GetAllFilesAsync();

        /// <summary>
        /// Returns file bytes with file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<byte[]> DownloadFileAsync(string fileName);

        /// <summary>
        /// Returns size of blob by file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<double> GetBlobSizeAsync(string fileName);

        /// <summary>
        /// Change sync file status
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<DNAFileModel> UpdateDnaFileSynchronizationStatusAsync(UpdateDnaFileSynchronizationStatusModel model);
    }
}
