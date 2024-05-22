using System;
using System.Threading.Tasks;
using Azure.Storage;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;
using WildHealth.Common.Options;

namespace WildHealth.Application.Utils.AzureBlobProvider;

public class InsuranceBlobProvider : IInsuranceBlobProvider
{ 
    private readonly InsuranceBlobOptions _insuranceBlobOptions;
    
    public InsuranceBlobProvider(IOptions<InsuranceBlobOptions> insuranceBlobOptions)
    {
        _insuranceBlobOptions = insuranceBlobOptions.Value;
    }

    /// <summary>
    /// <see cref="IInsuranceBlobProvider.GetBlobContainer"/>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public CloudBlobContainer GetBlobContainer(string name)
    {
        var blobClient = GetBlobClient();
        
        if (name.Contains("{0}"))
        {
            name = string.Format(name, _insuranceBlobOptions.Environment);
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
        throw new NotImplementedException();
    }

    #region Connection Initializing
    
    /// <summary>
    /// Retrieve storage account from connection string.
    /// </summary>
    /// <returns>CloudStorageAccount</returns>
    private CloudStorageAccount GetCloudStorageAccount()
    {
        var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_insuranceBlobOptions.Account};AccountKey={_insuranceBlobOptions.AccessKey};EndpointSuffix={_insuranceBlobOptions.EndpointSuffix}";

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