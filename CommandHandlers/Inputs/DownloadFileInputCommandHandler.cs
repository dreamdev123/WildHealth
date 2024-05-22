using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Inputs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class DownloadFileInputCommandHandler : IRequestHandler<DownloadFileInputCommand, (FileInput fileInput, byte[] content)>
    {
        private readonly IBlobFilesService _blobFilesService;
        private readonly IInputsService _inputsService;
        private readonly ILogger<DownloadFileInputCommandHandler> _logger;

        public DownloadFileInputCommandHandler(
            IBlobFilesService blobFilesService, 
            IInputsService inputsService, 
            ILogger<DownloadFileInputCommandHandler> logger)
        {
            _blobFilesService = blobFilesService;
            _inputsService = inputsService;
            _logger = logger;
        }

        public async Task<(FileInput fileInput, byte[] content)> Handle(DownloadFileInputCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Downloading of file input with id: {command.Id} has been started.");
            
            var fileInput = await _inputsService.GetFileInputAsync(
                id: command.Id,
                patientId: command.PatientId);

            if (fileInput.FileId is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(command.Id), command.Id);
                throw new AppException(HttpStatusCode.NotFound, "File doesn't contain blob data.", exceptionParam);
            }

            try
            {
                var content = await _blobFilesService.GetFileByIdAsync((int) fileInput.FileId);
                
                _logger.LogInformation($"Downloading of file input with id: {command.Id} has been finished.");

                return (fileInput, content);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Downloading of file input with id: {command.Id} has been failed. Error: ${ex.Message}.");
                
                throw;
            }
        }
    }
}