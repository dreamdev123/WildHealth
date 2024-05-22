using System;
using System.Threading.Tasks;
using Azure.Storage;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;
using WildHealth.Common.Options;

namespace WildHealth.Application.Utils.AzureBlobProvider
{
    /// <summary>
    /// <see cref="IAzureBlobProvider"/>
    /// </summary>
    public class AzureBlobProvider : IAzureBlobProvider
    {
        private readonly AzureBlobOptions _azureBlobOptions;

        public AzureBlobProvider(IOptions<AzureBlobOptions> azureBlobOptions)
        {
            _azureBlobOptions = azureBlobOptions.Value;
        }

        /// <summary>
        /// <see cref="IAzureBlobProvider.GetBlobContainer"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CloudBlobContainer GetBlobContainer(string name)
        {
            var blobClient = GetBlobClient();
            
            if (name.Contains("{0}"))
            {
                name = string.Format(name, _azureBlobOptions.Environment);
            }

            return blobClient.GetContainerReference(name);
        }

        public async Task<ICloudBlob> GetBlobReferenceAsync(string path)
        {
            var blobClient = GetBlobClient();
            return await blobClient.GetBlobReferenceFromServerAsync(new Uri(path));
        }

        public StorageSharedKeyCredential GetStorageSharedKeyCredential()
        {
            return new StorageSharedKeyCredential(_azureBlobOptions.Account, _azureBlobOptions.AccessKey);
        }
        
        #region Connection Initializing
        
        /// <summary>
        /// Retrieve storage account from connection string.
        /// </summary>
        /// <returns>CloudStorageAccount</returns>
        private CloudStorageAccount GetCloudStorageAccount()
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_azureBlobOptions.Account};AccountKey={_azureBlobOptions.AccessKey};EndpointSuffix={_azureBlobOptions.EndpointSuffix}";

            return CloudStorageAccount.Parse(connectionString);
        }

        /// <summary>
        /// Create the blob client
        /// </summary>
        /// <returns></returns>
        private CloudBlobClient GetBlobClient()
        {
            var storageAccount = GetCloudStorageAccount();
            return storageAccount.CreateCloudBlobClient();
        }
        
        #endregion Connection Initializing
    }
}