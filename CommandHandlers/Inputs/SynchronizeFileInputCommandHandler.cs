using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Extensions.BlobFiles;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Inputs;
using WildHealth.Domain.Entities.Files.Blobs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Data.Managers.TransactionManager;
using Microsoft.Extensions.Logging;
using WildHealth.Inputs.Services.InputsParser;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class SynchronizeFileInputCommandHandler : IRequestHandler<SynchronizeFileInputCommand, FileInput>
    {
        private readonly IInputsService _inputsService;
        private readonly IBlobFilesService _blobFilesService;
        private readonly ITransactionManager _transactionManager;
        private readonly IMediator _mediator;
        private readonly ILogger<SynchronizeFileInputCommandHandler> _logger;
        private readonly IInputsParser _inputsParser;
        
        public SynchronizeFileInputCommandHandler(
            IInputsService inputsService, 
            IBlobFilesService blobFilesService, 
            ITransactionManager transactionManager, 
            IMediator mediator, 
            ILogger<SynchronizeFileInputCommandHandler> logger,
            IInputsParser inputsParser)
        {
            _inputsService = inputsService;
            _blobFilesService = blobFilesService;
            _transactionManager = transactionManager;
            _mediator = mediator;
            _logger = logger;
            _inputsParser = inputsParser;
        }

        public async Task<FileInput> Handle(SynchronizeFileInputCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                $"Synchronizing of file input for patient with id: {command.PatientId} has been started.");

            var file = command.File;
            var inputType = command.Type;
            var dataProvider = command.DataProvider;
            var patientId = command.PatientId;
            var blobUri = command.BlobUri;
            var containerName = command.ContainerName;
            var bytes = await file.GetBytes();
            var fileName = file.GenerateStorageFileName(inputType, patientId, DateTime.Now);

            var aggregator = await _inputsService.GetAggregatorAsync(patientId, inputType);

            FileInput input;

            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                var blobFile = new BlobFile
                {
                    Uri = blobUri,
                    ContainerName = containerName,
                    Name = fileName,
                    MediaType = fileName.DeterminateContentType()
                };

                await _blobFilesService.CreateOrUpdateAsync(blobFile);

                var date = _inputsParser.ParseObservationDate(inputType, dataProvider, bytes);
                
                input = new FileInput(aggregator, blobFile, inputType, dataProvider, date);

                await _inputsService.CreateFileInputAsync(input);

                await _inputsService.FillOutInputsAsync(aggregator, input, bytes);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    $"Synchronizing of file input for patient with id: {command.PatientId} has been finished.");

                await _mediator.Publish(new FileInputsSynchronizedEvent(patientId, inputType, blobFile.Uri),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(
                    $"Synchronizing of file input for patient with id: {command.PatientId} has been failed. Error: ${ex.Message}.");

                throw;
            }

            return input;
        }
    }
}