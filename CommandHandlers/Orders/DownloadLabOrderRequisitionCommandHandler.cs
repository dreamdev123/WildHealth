using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.ClarityCore.WebClients.Labs;
using WildHealth.ClarityCore.Models.Labs;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Utils.PermissionsGuard;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Interfaces;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class DownloadLabOrderRequisitionCommandHandler : IRequestHandler<DownloadLabOrderRequisitionCommand, (byte[], string)>
    {
        private const string DefaultDocumentNamePattern = "Lab_Requisitions_{0}.pdf";
        private const string HtmlDocumentNamePattern = "Lab_Requisitions_{0}.html";
        private const string DefaultDocumentMediaType = "application/pdf";
        private const string HtmlDocumentMediaType = "text/html";
        private const string DocumentType = "REQ";
        
        private readonly ILabOrdersService _labOrdersService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ILabsWebClient _labsWebClient;
        private readonly ILogger _logger;

        public DownloadLabOrderRequisitionCommandHandler(
            ILabOrdersService labOrdersService, 
            IPermissionsGuard permissionsGuard,
            ILabsWebClient labsWebClient,
            ILogger<DownloadLabOrderRequisitionCommandHandler> logger)
        {
            _labOrdersService = labOrdersService;
            _permissionsGuard = permissionsGuard;
            _labsWebClient = labsWebClient;
            _logger = logger;
        }

        public async Task<(byte[], string)> Handle(DownloadLabOrderRequisitionCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Downloading of Lab Requisitions for order with [Id]: {command.OrderId} haw been started.");
            
            var order = await _labOrdersService.GetByIdAsync(command.OrderId);
            
            _permissionsGuard.AssertPermissions((IPatientRelated) order);

            var patientId = order.Patient.GetId();
            
            var report = await GetLabReportAsync(order);

            var document = GetRequisitionDocument(report);
            
            var bytes = await _labsWebClient.GetPatientLabDocumentFileAsync(patientId.ToString(), document.Id);

            _logger.LogInformation($"Downloading of Lab Requisitions for order with [Id]: {command.OrderId} haw been finished.");
            
            return (bytes, GenerateDocumentName(document, report.OrderNumber));
        }
        
        #region private

        /// <summary>
        /// Returns lab report
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<LabReportModel> GetLabReportAsync(LabOrder order)
        {
            var patientId = order.Patient.GetId();
            
            var reports = await _labsWebClient.GetPatientLabReportsAsync(patientId.ToString());

            var report = reports.Results
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault(x => x.OrderNumber == order.Number);

            if (report is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(order.Id), order.GetId());
                throw new AppException(HttpStatusCode.NotFound, "Requisition for order does not exist", exceptionParam);
            }

            return report;
        }

        /// <summary>
        /// Returns Lab Requisition document
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        private LabDocumentModel GetRequisitionDocument(LabReportModel report)
        {
            // Get requisition Document search first by pdf then by html as required in CLAR-1172

            var document = report.Documents.FirstOrDefault(x => x.MimeType == DefaultDocumentMediaType && x.Type == DocumentType);

            if (document is null)
            {
                document = report.Documents.FirstOrDefault(x => x.MimeType == HtmlDocumentMediaType && x.Type == DocumentType);
                if (document is null)
                {
                    throw new AppException(HttpStatusCode.NotFound, $"Requisition for order with number: {report.OrderNumber} does not exist");
                }
            }

            return document;
        }

        /// <summary>
        /// Generate and returns file name
        /// </summary>
        /// <param name="document"></param>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        private string GenerateDocumentName(LabDocumentModel document, string orderNumber)
        {
            if(document.MimeType == DefaultDocumentMediaType)
            {
                return string.Format(DefaultDocumentNamePattern, orderNumber);
            }

            return string.Format(HtmlDocumentNamePattern,orderNumber);
        }
        
        #endregion
    }
}