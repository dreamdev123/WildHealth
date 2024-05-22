using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Utils.AzureBlobProvider;

namespace WildHealth.Application.Services.AzureBlobService
{
    public class AzureBlobService : IAzureBlobService
    {
        private readonly IAzureBlobProvider _azureBlobProvider;
        private readonly ILogger<AzureBlobService> _logger;
        private readonly int _virtualNonExpiringUrlMinutes = int.MaxValue;

        #region Ctor
        
        public AzureBlobService(IAzureBlobProvider azureBlobProvider,
                                ILogger<AzureBlobService> logger)
        {
            _azureBlobProvider = azureBlobProvider;
            _logger = logger;
        }
        
        #endregion Ctor

        /// <summary>
        /// Create the container if it doesn't already exist.
        /// </summary>
        /// <param name="name">Container name</param>
        /// <param name="isPublic">Set public access if TRUE</param>
        public void CreateBlobContainer(string name, bool isPublic = false)
        {
            var container = _azureBlobProvider.GetBlobContainer(name);

            container.CreateIfNotExists();

            if (isPublic)
            {
                container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            }
        }

        /// <summary>
        /// Retrieve reference to a blob".
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public CloudBlockBlob GetBlobReference(string containerName, string blobName)
        {
            var container = _azureBlobProvider.GetBlobContainer(containerName);
            
            return container.GetBlockBlobReference(blobName);
        }

        /// <summary>
        /// Get Blob ByteArray
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public async Task<byte[]> GetBlobBytes(string containerName, string blobName)
        {
            try
            {
                var blockBlob = GetBlobReference(containerName, blobName);

                await using var ms = new MemoryStream();

                await blockBlob.DownloadToStreamAsync(ms);

                return ms.ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error loading blob {blobName} from {containerName}: {e.ToString()}");
                throw;
            }
        }
        
        /// <summary>
        /// Get Blob ByteArray by full path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<byte[]> GetBlobBytes(string path)
        {
            try
            {
                var blockBlob = await _azureBlobProvider.GetBlobReferenceAsync(path);

                await using var ms = new MemoryStream();

                await blockBlob.DownloadToStreamAsync(ms);

                return ms.ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error loading blob from {path}: {e.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Get Blob Properties such as Content-type
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public BlobProperties GetBlobProperties(string containerName, string blobName)
        {
            var blockBlob = GetBlobReference(containerName, blobName);
            blockBlob.FetchAttributes();
            var properties = blockBlob.Properties;
            return properties;
        }

        /// <summary>
        /// Create or overwrite the "blobName" blob with contents from a bytes "fileBytes"
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="fileBytes"></param>
        public async Task<string> CreateUpdateBlobBytes(string containerName, string blobName, byte[] fileBytes)
        {
            var blockBlob = GetBlobReference(containerName, blobName);

            await blockBlob.UploadFromByteArrayAsync(fileBytes, 0, fileBytes.Length);

            blockBlob = GetBlobReference(containerName, blobName);
            
            return blockBlob.Uri.ToString();
        }

        /// <summary>
        /// Retrieve the blobs and/or directories within it. To access the rich set of properties and methods for a returned IListBlobItem, you must cast it to a CloudBlockBlob, CloudPageBlob, or CloudBlobDirectory object.
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public IEnumerable<IListBlobItem> GetListBlobs(string containerName)
        {
            var container = _azureBlobProvider.GetBlobContainer(containerName);
            
            return container.ListBlobs();
        }

        public async Task<Uri> GetBlobSasUri(string path, int? expirationMinutes=null)
        {
            var blockBlob = await _azureBlobProvider.GetBlobReferenceAsync(path);

            return GetUriFromBlockBlob(blockBlob, expirationMinutes);
        }

        public Uri GetBlobSasUri(string containerName, string blobName, int? expirationMinutes=null)
        {
            try
            {
                var blockBlob = GetBlobReference(containerName, blobName);

                return GetUriFromBlockBlob(blockBlob, expirationMinutes);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error loading blob {blobName} from {containerName}: {e.ToString()}");
                throw;
            }
        }

        private Uri GetUriFromBlockBlob(ICloudBlob blockBlob, int? expirationMinutes=null)
        {
            var expirationMinutesCalculated = expirationMinutes ?? _virtualNonExpiringUrlMinutes;
            
            try
            {
                var blobSasBuilder = new BlobSasBuilder()
                {
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutesCalculated),
                    BlobContainerName = blockBlob.Container.Name,
                    BlobName = blockBlob.Name
                };
                
                blobSasBuilder.SetPermissions(BlobSasPermissions.Read);
                
                var storageSharedKeyCredential = _azureBlobProvider.GetStorageSharedKeyCredential();
                
                BlobSasQueryParameters sasQueryParameters = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential);

                BlobUriBuilder blobUriBuilder = new BlobUriBuilder(blockBlob.Uri)
                {
                    Sas = sasQueryParameters
                };

                return blobUriBuilder.ToUri();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error loading blob block {blockBlob.Name}: {e}");
                throw;
            }
        }

        /// <summary>
        /// Delete the blob.
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        public async Task<bool> DeleteBlobAsync(string containerName, string blobName)
        {
            var blockBlob = GetBlobReference(containerName, blobName);

            return await blockBlob.DeleteIfExistsAsync();
        }
    }
}