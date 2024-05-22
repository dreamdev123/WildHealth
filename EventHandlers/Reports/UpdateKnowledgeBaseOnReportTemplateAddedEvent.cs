using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Application.Domain.Reports;
using WildHealth.Application.Events.Reports;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Documents;
using WildHealth.Application.Services.Reports.Template;
using WildHealth.Common.Constants;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;

namespace WildHealth.Application.EventHandlers.Reports;

public class UpdateKnowledgeBaseOnReportTemplateAddedOrUpdatedEvent : INotificationHandler<ReportTemplateAddedOrUpdatedEvent>
{
    private const int ReportTemplateDocumentSourceTypeId = 5;

    private readonly IJennyKnowledgeBaseWebClient _jennyKnowledgeBaseWebClient;
    private readonly IReportTemplateService _reportTemplateService;
    private readonly IDocumentSourceTypesService _documentSourceTypesService;
    private readonly IDocumentSourcesService _documentSourcesService;
    private readonly IBlobFilesService _blobFilesService;
    private readonly MaterializeFlow _materialize;

    public UpdateKnowledgeBaseOnReportTemplateAddedOrUpdatedEvent(
        IJennyKnowledgeBaseWebClient jennyKnowledgeBaseWebClient,
        IReportTemplateService reportTemplateService,
        IDocumentSourceTypesService documentSourceTypesService,
        IDocumentSourcesService documentSourcesService,
        IBlobFilesService blobFilesService,
        MaterializeFlow materialize)
    {
        _jennyKnowledgeBaseWebClient = jennyKnowledgeBaseWebClient;
        _reportTemplateService = reportTemplateService;
        _documentSourceTypesService = documentSourceTypesService;
        _documentSourcesService = documentSourcesService;
        _blobFilesService = blobFilesService;
        _materialize = materialize;
    }

    public async Task Handle(ReportTemplateAddedOrUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var reportTemplate = await _reportTemplateService.GetByIdAsync(notification.ReportTemplateId);

        var documentSourceType = await _documentSourceTypesService.GetByIdAsync(ReportTemplateDocumentSourceTypeId);

        var documentSourceName = reportTemplate.GenerateDocumentSourceName();
        
        var existingDocumentSource = await _documentSourcesService.GetByNameAndTypeAsync(
            name: documentSourceName,
            documentSourceTypeId: documentSourceType.GetId());

        if (existingDocumentSource is not null)
        {
            // Delete existing document source which handles deleting chunks from VectorDB
            await new DeleteDocumentSourceFlow(existingDocumentSource).Materialize(_materialize);
        }

        var chunks = reportTemplate.GetGeneralChunks();

        var fileName = reportTemplate.GenerateFileName();

        var blobFile = await _blobFilesService.CreateOrUpdateWithBlobAsync(
            Encoding.UTF8.GetBytes(string.Join("\n", chunks)), fileName, AzureBlobContainers.KbDocuments);

        // We are directly creating the document source here instead of using AddDocumentSourceFlow because it's already chunked

        var documentSource = new DocumentSource(
            name: documentSourceName,
            documentSourceType: documentSourceType,
            file: blobFile)
        {
            DocumentChunks = chunks.Select(chunk =>
                new DocumentChunk(content: chunk, 
                    chunkingStrategy: DocumentChunkingStrategy.Other)
                {
                    Tags = Enumerable.Empty<int>().ToArray()
                }).ToArray()
        };

        documentSource = await _documentSourcesService.CreateAsync(documentSource);

        await _jennyKnowledgeBaseWebClient.StoreChunks(new DocumentChunkStoreRequestModel
        {
            Chunks = documentSource.DocumentChunks.Select((c) => new DocumentChunkStoreModel
            {
                Document = c.Content,
                ResourceId = c.UniversalId.ToString(),
                ResourceType = AiConstants.ResourceTypes.GeneralKnowledgeBaseChunk,
                Tags = c.Tags.Select(o => o.ToString()).ToArray(),
                Personas = Array.Empty<string>()
            }).ToArray()
        });
    }
}