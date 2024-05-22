using System.Threading.Tasks;
using Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace WildHealth.Application.Utils.AzureBlobProvider
{
    /// <summary>
    /// Provides method for working with azure blobs
    /// </summary>
    public interface IAzureBlobProvider
    {
        /// <summary>
        /// Retrieve a reference to a container.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        CloudBlobContainer GetBlobContainer(string name);

        /// <summary>
        /// Retrieve a reference to blob
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<ICloudBlob> GetBlobReferenceAsync(string path);

        /// <summary>
        /// Returns a StorageSharedKeyCredential for the account
        /// </summary>
        /// <returns></returns>
        StorageSharedKeyCredential GetStorageSharedKeyCredential();
    }
}