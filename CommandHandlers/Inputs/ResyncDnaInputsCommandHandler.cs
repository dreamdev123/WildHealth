using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Inputs;
using WildHealth.Common.Constants;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Shared.Data.Repository;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class ResyncDnaInputsCommandHandler: IRequestHandler<ResyncDnaInputsCommand>
    {
        private readonly IInputsService _inputsService;
        private readonly IGeneralRepository<FileInput> _fileInputs;
        private readonly IAzureBlobService _azureBlobService;
        private readonly ILogger _logger;

        public ResyncDnaInputsCommandHandler(
            IInputsService inputsService, 
            IGeneralRepository<FileInput> fileInputs, 
            IAzureBlobService azureBlobService, 
            ILogger<ResyncDnaInputsCommandHandler> logger)
        {
            _inputsService = inputsService;
            _fileInputs = fileInputs;
            _azureBlobService = azureBlobService;
            _logger = logger;
        }

        public async Task Handle(ResyncDnaInputsCommand request, CancellationToken cancellationToken)
        {
            var fileInputs = await _fileInputs
                .All()
                .Where(x => x.Type == FileInputType.DnaReport)
                .Include(x => x.Aggregator)
                .Include(x => x.File)
                .ToArrayAsync(cancellationToken: cancellationToken);

            _logger.LogInformation($"Resync DNA files started. Received ${fileInputs.Count()} files.");
            
            foreach (var fileInput in fileInputs)
            {
                try
                {
                    _logger.LogInformation($"Resync DNA file input with [Id] {fileInput.Id} started.");
                    
                    await ProcessFileInputAsync(fileInput, cancellationToken);
                    
                    _logger.LogInformation($"Resync DNA file input with [Id] {fileInput.Id} finished.");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Resync DNA file input with [Id] {fileInput.Id} failed. Error: {e.Message}.");
                }
            }
            
            _logger.LogInformation("Resync DNA files finished.");
        }
        
        #region private

        private async Task ProcessFileInputAsync(FileInput fileInput, CancellationToken cancellationToken)
        {
            var blob = _azureBlobService.GetBlobReference(AzureBlobContainers.PatientsInputs, fileInput.File.GetOriginName());
            if (blob is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Blob with name: {fileInput.File.GetOriginName()} does not exist.");
            }

            await using var stream = new MemoryStream();
                
            await blob.DownloadToStreamAsync(stream, cancellationToken);

            var bytes = stream.ToArray();
            
            var aggregator = await _inputsService.GetAggregatorAsync(fileInput.Aggregator.PatientId, fileInput.Type);

            await _inputsService.FillOutInputsAsync(aggregator, fileInput, bytes);
        }

        #endregion
    }
}