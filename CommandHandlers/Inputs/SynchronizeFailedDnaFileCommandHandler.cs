using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.DnaFiles;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.DNAFiles;
using WildHealth.Common.Models.Inputs;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Logs;
using WildHealth.Shared.Exceptions;
using CsvHelper;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs;

public class SynchronizeFailedDnaFileCommandHandler : IRequestHandler<SynchronizeFailedDnaFileCommand>
{
    private readonly ILogger<SynchronizeFailedDnaFileCommandHandler> _logger;
    private readonly IAzureBlobService _azureBlobService;
    private readonly IDnaOrdersService _dnaOrdersService;
    private readonly IDnaFilesService _dnaFilesService;
    private readonly IMediator _mediator;

    public SynchronizeFailedDnaFileCommandHandler(
        ILogger<SynchronizeFailedDnaFileCommandHandler> logger,
        IAzureBlobService azureBlobService,
        IDnaOrdersService dnaOrdersService,
        IDnaFilesService dnaFilesService,
        IMediator mediator
    )
    {
        _azureBlobService = azureBlobService;
        _dnaOrdersService = dnaOrdersService;
        _dnaFilesService = dnaFilesService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SynchronizeFailedDnaFileCommand request, CancellationToken cancellationToken)
    {
        var blob = GetBlobAsync(request.FileName);

        var records = await GetBlobRecords(blob);

        var practices = new List<(int practiceId, int locationId)>();

        foreach (var record in records)
        {
            _logger.LogInformation($"[Failed DNA Kits] trying to cancel addons for barcode: {record.Barcode}");

            try
            {
                var order = await _dnaOrdersService.GetByBarcodeAsync(record.Barcode);

                if (order is null)
                {
                    _logger.LogWarning($"[Failed DNA Kits] order for barcode {record.Barcode} does not exist");

                    continue;
                }

                var patient = order.Patient;

                await TryCloseDnaOrderAsync(order, cancellationToken);

                if (!practices.Contains((patient.User.PracticeId, patient.LocationId)))
                {
                    practices.Add((patient.User.PracticeId, patient.LocationId));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"[Failed DNA Kits] failed to cancel addons for barcode: {record.Barcode}: {e.Message}");

                throw;
            }
        }

        await AddSyncedFileRecordAsync(request.FileName);
    }

    private async Task<List<DnaKitErrorModel>> GetBlobRecords(CloudBlob blob)
    {
        var length = blob.Properties.Length;
        var bytes = new byte[length];
        await blob.DownloadToByteArrayAsync(bytes, 0);

        await using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Configuration.HasHeaderRecord = false;
        csv.Configuration.MissingFieldFound = null;
        csv.Configuration.Delimiter = ",";

        return csv.GetRecords<DnaKitErrorModel>().ToList();
    }

    private CloudBlob GetBlobAsync(string fileName)
    {
        var blob = _azureBlobService
            .GetListBlobs(AzureBlobContainers.FailedDnaKits)
            .OfType<CloudBlob>()
            .FirstOrDefault(b => b.Name.Contains(fileName));

        if (blob is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, $"[LabCorp Sync] No files found. FileName: {fileName}");
        }

        return blob;
    }

    private async Task AddSyncedFileRecordAsync(string fileName)
    {
        var syncDnaFileModel = new UpdateDnaFileSynchronizationStatusModel()
        {
            FileName = fileName,
            IsSynchronized = true,
            Type = SyncFileLogType.FailedDnaKit
        };

        await _dnaFilesService.UpdateDnaFileSynchronizationStatusAsync(syncDnaFileModel);
    }

    private async Task TryCloseDnaOrderAsync(DnaOrder order, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new CloseDnaOrderCommand(order.GetId(), false), cancellationToken);
        }
        catch (Exception err)
        {
            _logger.LogWarning($"[LabCorp Sync] Can't close delivery order, with [Error] : {err}");
        }
    }
}