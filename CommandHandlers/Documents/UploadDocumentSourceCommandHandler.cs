using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Extensions.BlobFiles;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Documents;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Files.Blobs;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Documents;

public class UploadDocumentSourceCommandHandler : IRequestHandler<UploadDocumentSourceCommand>
{
    private const string Container = AzureBlobContainers.KbDocuments;
    private readonly IDocumentSourceTypesService _documentSourceTypesService;
    private readonly IBlobFilesService _blobFilesService;
    private readonly MaterializeFlow _materialize;
    
    public UploadDocumentSourceCommandHandler(IDocumentSourceTypesService documentSourceTypesService, IBlobFilesService blobFilesService, MaterializeFlow materialize)
    {
        _documentSourceTypesService = documentSourceTypesService;
        _blobFilesService = blobFilesService;
        _materialize = materialize;
    }

    public async Task Handle(UploadDocumentSourceCommand command, CancellationToken cancellationToken)
    {
        var file = command.File;
        
        var documentSourceType = await _documentSourceTypesService.GetByIdAsync(command.DocumentSourceTypeId);

        BlobFile? blobFile = null;

        if (file is not null)
        {
            var bytes = await file.GetBytes();

            var fileName = file.GenerateStorageFileName(documentSourceType.Type);

            blobFile = await _blobFilesService.CreateOrUpdateWithBlobAsync(bytes, fileName, Container);
        }
        
        // Want to check if Tags are provided, if they are not, then we want to reach out to the Jenny service to tag a document
        var recommendedTags = Enumerable.Empty<HealthCategoryTag>().ToArray();

        await new AddDocumentSourceFlow(
            command.Name, 
            documentSourceType, 
            command.PersonaIds, 
            recommendedTags,
            command.Tags, 
            blobFile, 
            command.Url).Materialize(_materialize);
    }
}