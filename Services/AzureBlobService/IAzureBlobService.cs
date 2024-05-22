using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace WildHealth.Application.Services.AzureBlobService
{
    public interface IAzureBlobService
    {
        void CreateBlobContainer(string name, bool isPublic = false);
        
        CloudBlockBlob GetBlobReference(string containerName, string blobName);
        
        Task<byte[]> GetBlobBytes(string containerName, string blobName);

        Task<byte[]> GetBlobBytes(string path);

        BlobProperties GetBlobProperties(string containerName, string blobName);
        
        Task<string> CreateUpdateBlobBytes(string containerName, string blobName, byte[] fileBytes);
        
        IEnumerable<IListBlobItem> GetListBlobs(string containerName);

        Uri GetBlobSasUri(string containerName, string blobName, int? expirationMinutes=null);

        Task<Uri> GetBlobSasUri(string path, int? expirationMinutes=null);
        
        Task<bool> DeleteBlobAsync(string containerName, string blobName);
  
    }
}