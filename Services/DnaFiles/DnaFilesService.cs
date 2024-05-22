using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Tags;
using WildHealth.Application.Utils.DnaFiles;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.DNAFiles;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Logs;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Logs;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.DnaFiles
{
    /// <summary>
    /// <see cref="IDnaFilesService"/>
    /// </summary>
    public class DnaFilesService : IDnaFilesService
    {
        private readonly IGeneralRepository<SyncFileLog> _syncFileLogsRepository;
        private readonly IGeneralRepository<DnaOrder> _dnaOrdersRepository;
        private readonly ITagsService _tagsService;
        private readonly ITagRelationsService _tagRelationsService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly ILogger<DnaFilesService> _logger;
        private readonly string _dnaContainerName = AzureBlobContainers.PatientsInputs;

        public DnaFilesService(
            IGeneralRepository<SyncFileLog> syncFileLogsRepository,
            IGeneralRepository<DnaOrder> dnaOrdersRepository,
            ITagsService tagsService,
            ITagRelationsService tagRelationsService,
            ILogger<DnaFilesService> logger,
            IAzureBlobService azureBlobService)
        {
            _syncFileLogsRepository = syncFileLogsRepository;
            _azureBlobService = azureBlobService;
            _dnaOrdersRepository = dnaOrdersRepository;
            _tagsService = tagsService;
            _tagRelationsService = tagRelationsService;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IDnaFilesService.GetAllFilesAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DNAFileModel>> GetAllFilesAsync()
        {
            var failedOrders = await GetFailedOrdersAsync();
            var syncedFilesLogs = await GetSyncFilesLogsAsync();
            //var syncFileNames = syncedFilesLogs.Select(c => c.Name).ToArray();
            
            var blobNames = _azureBlobService.GetListBlobs(_dnaContainerName)
                .OfType<CloudBlockBlob>()
                .Where(blob => IsDNAFile(blob.Name))
                .Select(blob => blob.Name)
                .ToList();
            
            var taggedFiles = await GetTaggedFiles();

            var filesList = blobNames
                .Select(fileName => new DNAFileModel
                {
                    Name = fileName,
                    Date = DnaFilesHelper.GetDateFromFileName(fileName),
                    DisplayName = DnaFilesHelper.GetFileBarcode(fileName),
                    IsFileSynced = IsFileSynced(fileName, syncedFilesLogs, taggedFiles),
                    IsFileTagged = IsFileTagged(fileName, syncedFilesLogs, taggedFiles),
                    IsFileFailed = false
                }).Concat(failedOrders.Select(order => new DNAFileModel
                {
                    Name = order.Barcode,
                    Date = order.StatusLogs.Last(x => x.Status == OrderStatus.Failed).Date,
                    DisplayName = order.Barcode,
                    IsFileSynced = false,
                    IsFileTagged = false,
                    IsFileFailed = true
                })).ToArray();

            return filesList;
        }

        /// <summary>
        /// <see cref="IDnaFilesService.DownloadFileAsync(string)"/>
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<byte[]> DownloadFileAsync(string fileName)
        {
            return await _azureBlobService.GetBlobBytes(_dnaContainerName, fileName);
        }

        /// <summary>
        /// <see cref="IDnaFilesService.GetBlobSizeAsync(string)"/>
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<double> GetBlobSizeAsync(string fileName)
        {
            var blob = _azureBlobService.GetListBlobs(_dnaContainerName)
                .OfType<CloudBlob>()
                .FirstOrDefault(b => b.Name.Contains(fileName));

            if (blob is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"File with name: ${fileName} does not exist.");
            }

            await blob.FetchAttributesAsync();

            return ConvertBytesToMegabytes(blob.Properties.Length);
        }

        /// <summary>
        /// <see cref="IDnaFilesService.UpdateDnaFileSynchronizationStatusAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<DNAFileModel> UpdateDnaFileSynchronizationStatusAsync(UpdateDnaFileSynchronizationStatusModel model)
        {
            if (model.IsSynchronized) 
            {
                await MarkDnaFileAsSynchronizedAsync(model.FileName, model.Type);
            }
            else 
            {
                await MarkDnaFileAsNotSynchronizedAsync(model.FileName, model.Type);
            }

            return new DNAFileModel
            {
                Name = model.FileName,
                Date = DnaFilesHelper.GetDateFromFileName(model.FileName),
                DisplayName = DnaFilesHelper.GetFileBarcode(model.FileName),
                IsFileSynced = model.IsSynchronized,
                IsFileFailed = false
            };
        }

        #region private

        /// <summary>
        /// Returns synchronized files logs
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<SyncFileLog>> GetSyncFilesLogsAsync()
        {
            return await _syncFileLogsRepository
                .Get(x => x.Type == SyncFileLogType.LabCorpDna)
                .AsNoTracking()
                .ToArrayAsync();
        }

        /// <summary>
        /// Returns failed add-ons
        /// </summary>
        /// <returns></returns>
        private async Task<DnaOrder[]> GetFailedOrdersAsync()
        {
            return await _dnaOrdersRepository
                .Get(x => x.Status == OrderStatus.Failed)
                .IncludeOrderItemsWithAddOns()
                .AsNoTracking()
                .ToArrayAsync();
        }

        private async Task MarkDnaFileAsSynchronizedAsync(string fileName, SyncFileLogType type)
        {
            var syncFileLog = await _syncFileLogsRepository
                .All()
                .FirstOrDefaultAsync(x => x.Name == fileName && x.Type == type);

            if (!(syncFileLog is null))
            {
                return;
            }

            await _syncFileLogsRepository.AddAsync(new SyncFileLog
            {
                Name = fileName,
                Date = DateTime.UtcNow,
                Type = type
            });

            await _syncFileLogsRepository.SaveAsync();
        }

        private async Task MarkDnaFileAsNotSynchronizedAsync(string fileName, SyncFileLogType type)
        {
            var syncFileLog = await _syncFileLogsRepository
                .All()
                .FirstOrDefaultAsync(x => x.Name == fileName && x.Type == type);

            if (syncFileLog is null)
            {
                return;
            }

            _syncFileLogsRepository.Delete(syncFileLog);

            await _syncFileLogsRepository.SaveAsync();
        }

        private double ConvertBytesToMegabytes(long bytes)
        {
            return Math.Round((bytes / 1024f) / 1024f, 2);
        }

        private bool IsFileSynced(string fileName,  IEnumerable<SyncFileLog> syncedFiles, IEnumerable<Guid> taggedFiles)
        {
            // synced DNA file is a calculated field, so adding another calculation ...
            // - should be in SyncedLogFiles
            // - AND
            // - file name NOT be tagged as DnaFileNotMatched
            try
            {
                var isInSyncedFiles = syncedFiles.Any( (file ) =>
                {
                    var isTaggedFile  = taggedFiles.Any(taggedGuid => taggedGuid == file.UniversalId);
                    return file.Name == fileName && !isTaggedFile;

                });
                
                return isInSyncedFiles ;
            }
            catch (Exception err)
            {
                _logger.LogInformation($"Error calculating synced dna lab from file: {fileName} with error :{err.Message}");
                return false;
            }
        }

        private bool IsFileTagged(string fileName, IEnumerable<SyncFileLog> syncedFiles, IEnumerable<Guid> taggedFiles)
        {
            
            // this validation is just for tagging purpose on FW
            try
            {
                var syncedFile = syncedFiles.Where(file => file.Name == fileName).FirstOrDefault();

                if (syncedFile is null)
                {
                    return false;
                } 
                var isTaggedFile  = taggedFiles.Any(taggedGuid => taggedGuid == syncedFile.UniversalId);

                return isTaggedFile;
            }
            catch (Exception err)
            {
                _logger.LogInformation($"Error getting tag from file: {fileName} with error :{err.Message}");
                return false;
            }
            
        }
        
        private bool IsDNAFile(string fileName)
        {
            return fileName.Contains("NC");
        }

        private async Task<IEnumerable<Guid>> GetTaggedFiles()
        {
            // get all tagged files with DNA not matched
            var allTaggedFiles = await _tagRelationsService.GetAllOfTag(TagsConstants.DnaFileNotMatched);
            return allTaggedFiles.Select(tagRelation => tagRelation.UniqueGuid).ToArray();
        }


        #endregion
    }
}
