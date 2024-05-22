using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Inputs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Data.Managers.TransactionManager;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Application.Extensions.BlobFiles;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class UploadInputsCommandHandler : IRequestHandler<UploadInputsCommand, FileInput>
    {
        private readonly IInputsService _inputsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public UploadInputsCommandHandler(
            IInputsService inputsService,
            ITransactionManager transactionManager, 
            IMediator mediator, 
            ILogger<UploadInputsCommandHandler> logger)
        {
            _inputsService = inputsService;
            _transactionManager = transactionManager;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<FileInput> Handle(UploadInputsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Uploading of input for patient with id: {command.PatientId} has been started.");

            var file = command.File;
            var inputType = command.Type;
            var dataProvider = command.DataProvider;
            var patientId = command.PatientId;
            var bytes = await file.GetBytes();
            var fileName = file.GenerateStorageFileName(inputType, patientId, DateTime.Now);

            var uploadInputsFileCommand = new UploadInputsFileCommand(
                type: inputType,
                dataProvider: dataProvider,
                bytes: bytes,
                fileName: fileName,
                patientId: patientId
            );

            var aggregator = await _inputsService.GetAggregatorAsync(patientId, inputType);

            FileInput input;

            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                input = await _mediator.Send(uploadInputsFileCommand, cancellationToken);

                await _inputsService.FillOutInputsAsync(aggregator, input, bytes);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    $"Uploading of input for patient with id: {command.PatientId} has been finished.");

                await _mediator.Publish(new FileInputsUploadedEvent(patientId, inputType, input.File.Uri),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(
                    $"Uploading of input for patient with id: {command.PatientId} has been failed. Error: ${ex.Message}.");

                throw;
            }

            return input;
        }
    }
}