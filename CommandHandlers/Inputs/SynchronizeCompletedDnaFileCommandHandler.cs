using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage.Blob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.DnaFiles;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Application.Services.Tags;
using WildHealth.Application.Utils.DnaFiles;
using WildHealth.ClarityCore.WebClients.Labs;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.DNAFiles;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Logs;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Entities.Tags;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Domain.Models.Patient;

namespace WildHealth.Application.CommandHandlers.Inputs;

public class SynchronizeCompletedDnaFileCommandHandler : IRequestHandler<SynchronizeCompletedDnaFileCommand>
{
    private readonly ILogger<SynchronizeCompletedDnaFileCommandHandler> _logger;
    private readonly IAzureBlobService _azureBlobService;
    private readonly IDnaOrdersService _dnaOrdersService;
    private readonly IDnaFilesService _dnaFilesService;
    private readonly ILabsWebClient _labsWebClient;
    private readonly ITagsService _tagsService;
    private readonly ITagRelationsService _tagRelationsService;
    private readonly IGeneralRepository<SyncFileLog> _syncFileInfoRepository;
    private readonly IMediator _mediator;

    public SynchronizeCompletedDnaFileCommandHandler(
        ILogger<SynchronizeCompletedDnaFileCommandHandler> logger,
        IAzureBlobService azureBlobService,
        IDnaOrdersService dnaOrdersService,
        IDnaFilesService dnaFilesService,
        ILabsWebClient labsWebClient,
        ITagsService tagsService,
        ITagRelationsService tagRelationsService,
        IGeneralRepository<SyncFileLog> syncFileInfoRepository,
        IMediator mediator
    )
    {
        _azureBlobService = azureBlobService;
        _dnaOrdersService = dnaOrdersService;
        _dnaFilesService = dnaFilesService;
        _labsWebClient = labsWebClient;
        _mediator = mediator;
        _logger = logger;
        _tagsService = tagsService;
        _tagRelationsService = tagRelationsService;
        _syncFileInfoRepository = syncFileInfoRepository;
    }

    public async Task Handle(SynchronizeCompletedDnaFileCommand request, CancellationToken cancellationToken)
    {
        var blob = GetBlobAsync(request.FileName);

        var order = await GetDnaOrderAsync(request.FileName);

        if (await DnaFileMatchWithPatient(blob, order))
        {
            await SyncFileInputsAsync(blob, order, cancellationToken);
            await AddSyncedFileRecordAsync(request.FileName);
            await TryCloseDnaOrderAsync(order, cancellationToken);
        }
        else
        {
            await TagDnaFileAsNotMatched(request.FileName);
        }
    }

    private async Task<DnaOrder> GetDnaOrderAsync(string fileName)
    {
        var fileBarcode = DnaFilesHelper.GetFileBarcode(fileName);

        return await _dnaOrdersService.GetByBarcodeAsync(fileBarcode);
    }

    private CloudBlob GetBlobAsync(string fileName)
    {
        var blob = _azureBlobService
            .GetListBlobs(AzureBlobContainers.PatientsInputs)
            .OfType<CloudBlob>()
            .FirstOrDefault(b => b.Name.Contains(fileName));

        if (blob is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, $"[LabCorp Sync] No files found. FileName: {fileName}");
        }

        return blob;
    }

    private async Task SyncFileInputsAsync(CloudBlob blob, DnaOrder order, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await blob.DownloadToStreamAsync(stream, cancellationToken);

        var formFile = new FormFile(
            baseStream: stream,
            baseStreamOffset: 0,
            length: stream.Length,
            name: blob.Name,
            fileName: blob.Name);

        var syncInputCommand = new SynchronizeFileInputCommand(
            type: FileInputType.DnaReport,
            dataProvider: FileInputDataProvider.LabCorpElation,
            file: formFile,
            blobUri: blob.Uri.ToString(),
            containerName: AzureBlobContainers.PatientsInputs,
            patientId: order.PatientId
        );

        await _mediator.Send(syncInputCommand, cancellationToken);
    }

    private async Task AddSyncedFileRecordAsync(string fileName)
    {
        var syncDnaFileModel = new UpdateDnaFileSynchronizationStatusModel()
        {
            FileName = fileName,
            IsSynchronized = true
        };

        await _dnaFilesService.UpdateDnaFileSynchronizationStatusAsync(syncDnaFileModel);
    }

    private async Task TryCloseDnaOrderAsync(DnaOrder order, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new CloseDnaOrderCommand(order.GetId(), true), cancellationToken);
        }
        catch (Exception err)
        {
            _logger.LogWarning($"[LabCorp Sync] Can't close delivery order, with [Error] : {err}");
        }
    }

    private async Task<bool> DnaFileMatchWithPatient(CloudBlob blob, DnaOrder order)
    {
        var parsedResponse = await _labsWebClient.ParseLabCorpMetadata(order.Barcode, blob.Uri.AbsoluteUri);

        var gender = parsedResponse["gender"];
        var barcode = parsedResponse["barcode"];
        var birthday = parsedResponse["birthday"];
        var patientDomain = PatientDomain.Create(order.Patient);

        var isBarcodeMatched = order.Barcode.Equals(barcode, StringComparison.InvariantCultureIgnoreCase);
        var isGenderMatched = patientDomain.IsSameGender(gender);
        var isBirthdayMatched = patientDomain.IsSameBirthdayDate(birthday);

        var allFieldMatched = isBarcodeMatched && isGenderMatched && isBirthdayMatched;

        if (!allFieldMatched)
        {
            LogWarningWhichFieldNotMatched(
                fileName: blob.Name,
                barcodeMatched: isBarcodeMatched,
                birthdayMatched: isBirthdayMatched,
                genderMatched: isGenderMatched, 
                order: order, 
                birthday: birthday, 
                barcode: barcode, 
                gender: gender,
                patientDomain : patientDomain
            );
        }

        return allFieldMatched;
    }

    private async Task TagDnaFileAsNotMatched(string fileName)
    {
        var syncFile = await _syncFileInfoRepository
            .All()
            .FirstOrDefaultAsync(f => f.Name == fileName);

        if (syncFile is null)
        {
            return;
        }
        
        var tag = await _tagsService.GetOrCreate(new Tag(
            name: TagsConstants.DnaFileNotMatched,
            description: "DNA file does not match with patient"
        ));
        
        if (tag is not null)
        {
            await _tagRelationsService.GetOrCreate(new TagRelation(
                tagId: tag.GetId(),
                uniqueGuid: syncFile.UniversalId
            ));
        }
    }

    private void LogWarningWhichFieldNotMatched(
        string fileName, 
        bool barcodeMatched,
        bool birthdayMatched, 
        bool genderMatched, 
        DnaOrder order,
        string birthday, 
        string barcode, 
        string gender,
        PatientDomain patientDomain)
    {
        if (!barcodeMatched)
        {
            _logger.LogWarning($"Dna Lab Sync field does not match for file {fileName}, [Barcode] : {barcode} from parse, {order.Barcode} from Order");
        }

        if (!birthdayMatched)
        {
            _logger.LogWarning($"Dna Lab Sync field does not match for file {fileName}, [Birthday] : {birthday} from parse, {patientDomain.GetBirthDay()} from Order");
        }

        if (!genderMatched)
        {
            _logger.LogWarning($"Dna Lab Sync field does not match for file {fileName}, [Gender] : {gender} from parse, {patientDomain.GetGenderName()} from Order");
        }
    }
}