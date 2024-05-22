using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Application.Domain.Reports;
using WildHealth.Application.Events.Reports;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Documents;

namespace WildHealth.Application.EventHandlers.Reports;

public class UpdateKnowledgeBaseOnReportTemplateDeletedEvent : INotificationHandler<ReportTemplateDeletedEvent>
{
    private const int ReportTemplateDocumentSourceTypeId = 5;
    
    private readonly IDocumentSourcesService _documentSourcesService;
    private readonly IDocumentSourceTypesService _documentSourceTypesService;
    private readonly MaterializeFlow _materialize;

    public UpdateKnowledgeBaseOnReportTemplateDeletedEvent(
        IDocumentSourcesService documentSourcesService, 
        IDocumentSourceTypesService documentSourceTypesService,
        MaterializeFlow materialize)
    {
        _documentSourcesService = documentSourcesService;
        _documentSourceTypesService = documentSourceTypesService;
        _materialize = materialize;
    }
    
    public async Task Handle(ReportTemplateDeletedEvent notification, CancellationToken cancellationToken)
    {
        var documentSourceName = notification.ReportTemplate.GenerateDocumentSourceName();
        
        var documentSourceType = await _documentSourceTypesService.GetByIdAsync(ReportTemplateDocumentSourceTypeId);
        
        var existingDocumentSource = await _documentSourcesService.GetByNameAndTypeAsync(
            name: documentSourceName,
            documentSourceTypeId: documentSourceType.GetId());

        if (existingDocumentSource is not null)
        {
            // Delete existing document source which handles deleting chunks from VectorDB
            await new DeleteDocumentSourceFlow(existingDocumentSource).Materialize(_materialize);
        }
    }
}