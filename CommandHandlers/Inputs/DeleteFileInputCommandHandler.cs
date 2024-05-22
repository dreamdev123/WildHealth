using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Inputs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Data.Managers.TransactionManager;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Inputs;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class DeleteFileInputCommandHandler : IRequestHandler<DeleteFileInputCommand, FileInput>
    {
        private readonly IBlobFilesService _blobFilesService;
        private readonly IInputsService _inputsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public DeleteFileInputCommandHandler(
            IBlobFilesService blobFilesService, 
            IInputsService inputsService, 
            ITransactionManager transactionManager, 
            IMediator mediator,
            ILogger<DeleteFileInputCommandHandler> logger)
        {
            _blobFilesService = blobFilesService;
            _inputsService = inputsService;
            _transactionManager = transactionManager;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task<FileInput> Handle(DeleteFileInputCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Deleting of file input with id: {command.Id} has been started.");
            
            var fileInput = await _inputsService.GetFileInputAsync(
                id: command.Id,
                patientId: command.PatientId
            );

            await using var transaction = _transactionManager.BeginTransaction();
            
            try
            {
                await _inputsService.DeleteFileInputAsync(fileInput);
                
                await _blobFilesService.DeleteAsync(fileInput.File.GetId());
                
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                
                _logger.LogInformation($"Deleting of file input with id: {command.Id} has been failed. Error: ${ex.Message}");
                
                throw;
            }
            
            _logger.LogInformation($"Deleting of file input with id: {command.Id} has been finished.");
            
            await _mediator.Publish(new FileInputDeletedEvent(command.PatientId, fileInput.Type), cancellationToken);

            return fileInput;
        }
    }
}