using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Files.Blobs;

namespace WildHealth.Application.Services.BlobFiles
{
    /// <summary>
    /// Provides methods for working with blob files
    /// </summary>
    public interface IBlobFilesService
    {
        /// <summary>
        /// Returns a BlobFile record by Id.
        /// </summary>
        /// <param name="id">BlobFile Id.</param>
        /// <returns>BlobFile record with given Id if exists and null otherwise.</returns>
        Task<BlobFile> GetByIdAsync(int id);

        /// <summary>
        /// Returns file data in bytes by BlobFile Id
        /// </summary>
        /// <param name="id">BlobFile Id.</param>
        /// <returns>Byte representation of image data</returns>
        Task<byte[]> GetFileByIdAsync(int id);

        /// <summary>
        /// Gets file data by BlobFile Id as encoded base64 string
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<string?> GetFileByIdEncodedAsync(int id);

        /// <summary>
        /// Gets BlobFile records with filter query.
        /// </summary>
        /// <param name="predicate">An expression which specifies the condition</param>
        /// <returns>List of all BlobFile records.</returns>
        Task<IEnumerable<BlobFile>> GetAsync(Expression<Func<BlobFile, bool>> predicate);

        /// <summary>
        /// Create or update a record if exists
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<BlobFile> CreateOrUpdateAsync(BlobFile file);

        /// <summary>
        /// Create or update a record with setting external blob data
        /// </summary>
        /// <param name="fileBytes"></param>
        /// <param name="fileName"></param>
        /// <param name="containerName"></param>
        /// <param name="blobFileId"></param>
        /// <returns></returns>
        Task<BlobFile> CreateOrUpdateWithBlobAsync(byte[] fileBytes, string fileName, string containerName, int blobFileId = 0);

        /// <summary>
        /// Deletes a record
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(int id);
    }
}