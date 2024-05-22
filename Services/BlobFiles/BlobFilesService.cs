using System;
using System.Net;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.BlobFiles;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Files.Blobs;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.BlobFiles
{
    public class BlobFilesService : IBlobFilesService
    {
        private readonly IGeneralRepository<BlobFile> _blobRepository;
        private readonly IAzureBlobService _azureBlobService;

        #region Ctor


        /// <summary>
        /// Creates an instance of BlobFilesService.
        /// </summary>
        /// <param name="blobRepository"></param>
        /// <param name="azureBlobService"></param>
        public BlobFilesService(
            IGeneralRepository<BlobFile> blobRepository,
            IAzureBlobService azureBlobService)
        {
            _blobRepository = blobRepository;
            _azureBlobService = azureBlobService;
        }

        #endregion

        /// <summary>
        /// <see cref="IBlobFilesService.GetByIdAsync(int)"/>
        /// </summary>
        public async Task<BlobFile> GetByIdAsync(int id)
        {
            var blobFile = await _blobRepository.GetAsync(id);
            if (blobFile is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "File does not exist", exceptionParam);
            }

            return blobFile;
        }

        /// <summary>
        /// <see cref="IBlobFilesService.GetFileByIdAsync(int)"/>
        /// </summary>
        public async Task<byte[]> GetFileByIdAsync(int id)
        {
            var record = await GetByIdAsync(id);
            return await _azureBlobService.GetBlobBytes(record.ContainerName, record.GetOriginName());
        }

        /// <summary>
        /// <see cref="IBlobFilesService.GetFileByIdEncodedAsync(int)"/>
        /// </summary>
        public async Task<string?> GetFileByIdEncodedAsync(int id)
        {
            var fileBytes = await GetFileByIdAsync(id);

            if (fileBytes == null)
            {
                return null;
            }
            
            var result = Convert.ToBase64String(fileBytes);
            return new Regex("(?im)A+==+$").Replace(result, "");
        }

        /// <summary>
        /// <see cref="IBlobFilesService.GetAsync"/>
        /// </summary>
        public async Task<IEnumerable<BlobFile>> GetAsync(Expression<Func<BlobFile, bool>> predicate)
        {
            var blobs = await _blobRepository
                .Get(predicate)
                .AsNoTracking()
                .ToArrayAsync();

            return blobs;
        }

        /// <summary>
        /// <see cref="IBlobFilesService.CreateOrUpdateAsync(BlobFile)"/>
        /// </summary>
        public async Task<BlobFile> CreateOrUpdateAsync(BlobFile file)
        {
            try
            {
                if (file.Id is null)
                {
                    throw new AppException(HttpStatusCode.NotFound, "File does not exist");
                }
                
                var existingBlobFile = await GetByIdAsync(file.GetId());
                
                existingBlobFile.Update(file);
                
                _blobRepository.Edit(file);
                
                await _blobRepository.SaveAsync();
            }
            catch (AppException e) when(e.StatusCode == HttpStatusCode.NotFound)
            {
                await _blobRepository.AddAsync(file);
                
                await _blobRepository.SaveAsync();
            }

            return file;
        }

        /// <summary>
        /// <see cref="IBlobFilesService.CreateOrUpdateWithBlobAsync"/>
        /// </summary>
        public async Task<BlobFile> CreateOrUpdateWithBlobAsync(byte[] fileBytes, string fileName, string containerName, int blobFileId = 0)
        {
            try
            {
                var existingBlobFile = await GetByIdAsync(blobFileId);
                
                existingBlobFile.Name = fileName;
                existingBlobFile.MediaType = fileName.DeterminateContentType();
                existingBlobFile.ContainerName = containerName;
                existingBlobFile.Uri = await _azureBlobService.CreateUpdateBlobBytes(containerName, fileName, fileBytes);
                
                _blobRepository.Edit(existingBlobFile);
                
                await _blobRepository.SaveAsync();
                
                return existingBlobFile;
            }
            catch (AppException e) when(e.StatusCode == HttpStatusCode.NotFound)
            {
                var blobFile = new BlobFile
                {
                    Name = fileName,
                    ContainerName = containerName,
                    MediaType = fileName.DeterminateContentType(),
                    Uri = await _azureBlobService.CreateUpdateBlobBytes(containerName, fileName, fileBytes)
                };

                await _blobRepository.AddAsync(blobFile);
                
                await _blobRepository.SaveAsync();
                
                return blobFile;
            }
        }

        /// <summary>
        /// <see cref="IBlobFilesService.DeleteAsync"/>
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var blobFile = await GetByIdAsync(id);
                
                _blobRepository.Delete(blobFile);
                
                await _blobRepository.SaveAsync();
                
                return true;
            }
            catch(AppException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}