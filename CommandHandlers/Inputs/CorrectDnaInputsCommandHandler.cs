using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Inputs;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Logs;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Enums.Logs;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Data.Repository;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs;

public class CorrectDnaInputsCommandHandler : IRequestHandler<CorrectDnaInputsCommand>
{
    private readonly IGeneralRepository<SyncFileLog> _syncFileInfoRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IAzureBlobService _azureBlobService;
    private readonly IDnaOrdersService _ordersService;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public CorrectDnaInputsCommandHandler(
        IGeneralRepository<SyncFileLog> syncFileInfoRepository, 
        ITransactionManager transactionManager, 
        IAzureBlobService azureBlobService, 
        IDnaOrdersService ordersService,
        IMediator mediator, 
        ILogger<CorrectDnaInputsCommandHandler> logger)
    {
        _syncFileInfoRepository = syncFileInfoRepository;
        _transactionManager = transactionManager;
        _azureBlobService = azureBlobService;
        _ordersService = ordersService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(CorrectDnaInputsCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = _transactionManager.BeginTransaction();
        
        try
        {
            var order = await _ordersService.GetByBarcodeAsync(command.Barcode);
                
            _logger.LogInformation($"Started correcting DNA inputs based on barcode: {command.Barcode}");

            var blobs = _azureBlobService
                .GetListBlobs(AzureBlobContainers.PatientsInputs)
                .OfType<CloudBlob>()
                .Where(b => b.Name.Contains(command.Barcode))
                .ToArray();

            if (!blobs.Any())
            {
                _logger.LogInformation($"Didn't find any dna files with barcode: {command.Barcode}");

                return;
            }
            
            if (blobs.Length == 1)
            {
                _logger.LogInformation($"Didn't find corrected dna file with barcode: {command.Barcode}");

                return;
            }
                
            var lastBlob = blobs
                .OrderByDescending(x => x.Properties.Created)
                .First();
                
            await using var stream = new MemoryStream();
            await lastBlob.DownloadToStreamAsync(stream, cancellationToken);
            var formFile = new FormFile(
                baseStream: stream,
                baseStreamOffset: 0,
                length: stream.Length,
                name: string.Empty,
                fileName: lastBlob.Name);

            var syncInputCommand = new SynchronizeFileInputCommand(
                type: FileInputType.DnaReport,
                dataProvider: FileInputDataProvider.LabCorpElation,
                file: formFile,
                blobUri: lastBlob.Uri.ToString(),
                containerName: AzureBlobContainers.PatientsInputs,
                patientId: order.PatientId
            );

            await _mediator.Send(syncInputCommand, cancellationToken);

            await _syncFileInfoRepository.AddAsync(new SyncFileLog
            {
                Name = lastBlob.Name,
                Date = DateTime.UtcNow,
                Type = SyncFileLogType.LabCorpDna
            });

            await _syncFileInfoRepository.SaveAsync();

            await transaction.CommitAsync(cancellationToken);
        }
        catch (AggregateException e)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            _logger.LogError($"Started correcting DNA inputs based on barcode: {command.Barcode}, {e.InnerException?.Message}");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            _logger.LogError($"Started correcting DNA inputs based on barcode: {command.Barcode}, {e.Message}");
        }
    }
}