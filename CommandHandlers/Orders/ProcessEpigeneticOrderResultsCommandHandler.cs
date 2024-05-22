using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Epigenetic;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.TrueDiagnostic.WebClients;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Exceptions;
using WildHealth.Application.CommandHandlers.Orders.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Utils.Converter;
using WildHealth.TrueDiagnostic.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders;

public class ProcessEpigeneticOrderResultsCommandHandler : IRequestHandler<ProcessEpigeneticOrderResultsCommand>
{
    private readonly IEpigeneticOrdersService _epigeneticOrdersService;
    private readonly ITrueDiagnosticWebClient _trueDiagnosticWebClient;
    private readonly IFlowMaterialization _materializeFlow;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPatientsService _patientsService;
    private readonly IConverterUtil _converterUtil;
    private readonly IMediator _mediator;

    public ProcessEpigeneticOrderResultsCommandHandler(
        IEpigeneticOrdersService epigeneticOrdersService, 
        ITrueDiagnosticWebClient trueDiagnosticWebClient, 
        IFlowMaterialization materializeFlow, 
        IDateTimeProvider dateTimeProvider, 
        IPatientsService patientsService, 
        IConverterUtil converterUtil,
        IMediator mediator)
    {
        _epigeneticOrdersService = epigeneticOrdersService;
        _trueDiagnosticWebClient = trueDiagnosticWebClient;
        _materializeFlow = materializeFlow;
        _dateTimeProvider = dateTimeProvider;
        _patientsService = patientsService;
        _converterUtil = converterUtil;
        _mediator = mediator;
    }

    public async Task Handle(ProcessEpigeneticOrderResultsCommand command, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.Empty);

        var order = await _epigeneticOrdersService.GetAsync(command.OrderId);

        ValidateOrder(order, command);

        var documents = await DownloadDocumentsAsync(order);

        await UploadDocumentsAsync(documents, patient);

        await MarkOrderAsCompletedAsync(order);
    }
    
    #region private

    private void ValidateOrder(EpigeneticOrder order, ProcessEpigeneticOrderResultsCommand command)
    {
        if (order.PatientId != command.PatientId)
        {
            throw new DomainException("Patient doesn't match");
        }
        
        if (order.Number != command.OrderNumber)
        {
            throw new DomainException("Order number doesn't match");
        }
        
        if (order.Status == OrderStatus.Completed)
        {
            throw new DomainException("Order completed already");
        }
    }

    private async Task<IFormFile[]> DownloadDocumentsAsync(EpigeneticOrder order)
    {
        var reportIds = await _trueDiagnosticWebClient.GetReportIds(order.Number);

        var allFiles = new List<IFormFile>();

        foreach (var (reportId, format) in reportIds)
        {
            try
            {
                var result = await _trueDiagnosticWebClient.DownloadReport(reportId, format);

                var files = format == "pdf"
                    ? result.ReportList.Select(x => _converterUtil.ConvertBase64ToFormFile(x.ReportData, GenerateReportName(x.ReportName, format)))
                    : result.ReportList.Select(x => _converterUtil.ConvertStringToFormFile(x.ReportData, GenerateReportName(x.ReportName, format)));

                allFiles.AddRange(files);
            }
            catch(TrueDiagnosticException ex) when(ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Ignore exception if file does not exist.
                // According to True Diagnostic logic it's possible scenario
            }
        }

        return allFiles.ToArray();
    }

    private Task UploadDocumentsAsync(IFormFile[] documents, Patient patient)
    {
        var command = new UploadDocumentsCommand(
            documents: documents,
            attachmentType: AttachmentType.EpigeneticReports,
            patientId: patient.GetId(),
            uploadedByUserId: patient.UserId,
            isSendToKb: true
        );

        return _mediator.Send(command);
    }

    private string GenerateReportName(string fileName, string format)
    {
        if (fileName.EndsWith("." + format)) return fileName;

        return $"{fileName}.{format}";
    }

    private Task MarkOrderAsCompletedAsync(EpigeneticOrder order)
    {
        var flow = new MarkEpigeneticOrderAsCompletedFlow(order, _dateTimeProvider.UtcNow());

        return flow.Materialize(_materializeFlow.Materialize);
    }
    
    #endregion
}