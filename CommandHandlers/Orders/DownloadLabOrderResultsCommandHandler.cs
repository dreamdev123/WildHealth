using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Application.Commands.Orders;
using WildHealth.ClarityCore.Models.Labs;
using WildHealth.ClarityCore.WebClients.Labs;
using WildHealth.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class DownloadLabOrderResultsCommandHandler : IRequestHandler<DownloadLabOrderResultsCommand, (byte[], string)>
    {
        private const string DocumentType = "LABRES";

        /// <summary>
        /// This values is going in the right priority
        /// Priority:
        ///     * CORRECTED
        ///     * FINAL
        /// </summary>
        private static readonly string[] DocumentStatuses =
        {
            "CORRECTED",
            "FINAL"
        };
        
        /// <summary>
        /// This values is going in the right priority
        /// Priority:
        ///     * PDF
        ///     * HTML
        /// </summary>
        private static readonly string[] DocumentMediaTypes =
        {
            "application/pdf",
            "text/html",
        };

        private static readonly IDictionary<string, string> DocumentNamePatterns = new Dictionary<string, string>
        {
            {
                "application/pdf",
                "Lab_Results_{0}.pdf"
            },
            {
                "text/html",
                "Lab_Results_{0}.html"
            }
        };
        
        private readonly ILabsWebClient _labsWebClient;
        private readonly ILogger _logger;

        public DownloadLabOrderResultsCommandHandler(
            ILabsWebClient labsWebClient,
            ILogger<DownloadLabOrderResultsCommandHandler> logger)
        {
            _labsWebClient = labsWebClient;
            _logger = logger;
        }

        public async Task<(byte[], string)> Handle(DownloadLabOrderResultsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Downloading of Lab Results for patient with [Id]: {command.PatientId} haw been started.");

            var patientId = command.PatientId;

            var reportId = command.ReportId;
            
            var report = await GetLabReportAsync(patientId, reportId);

            var document = GetResultsDocument(report);
            
            var bytes = await _labsWebClient.GetPatientLabDocumentFileAsync(patientId.ToString(), document.Id);
            
            _logger.LogInformation($"Downloading of Lab Results for patient with [Id]: {command.PatientId} haw been finished.");

            return (bytes, GenerateDocumentName(document, report.OrderNumber));
        }
        
        #region private

        /// <summary>
        /// Returns lab report
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<LabReportModel> GetLabReportAsync(int patientId, int reportId)
        {
            var report = await _labsWebClient.GetPatientLabReportAsync(patientId.ToString(), reportId);

            if (report is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(reportId), reportId);
                throw new AppException(HttpStatusCode.NotFound, "Results for report don't exist", exceptionParam);
            }

            return report;
        }

        /// <summary>
        /// Returns Lab results document
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        private LabDocumentModel GetResultsDocument(LabReportModel report)
        {
            foreach (var status in DocumentStatuses)
            {
                foreach (var mediaType in DocumentMediaTypes)
                {
                    var document = report.Documents
                        .OrderByDescending(x => x.CreatedAt)
                        .FirstOrDefault(x => 
                            string.Equals(x.MimeType, mediaType, StringComparison.CurrentCultureIgnoreCase)
                            && string.Equals(x.Type, DocumentType, StringComparison.CurrentCultureIgnoreCase) 
                            && string.Equals(x.Status, status, StringComparison.CurrentCultureIgnoreCase));

                    if (document is null)
                    {
                        continue;
                    }

                    return document;
                }
            }

            throw new AppException(HttpStatusCode.NotFound, $"Results for order with number: {report.OrderNumber} does not exist");
        }

        /// <summary>
        /// Generate and returns file name
        /// </summary>
        /// <param name="document"></param>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        private string GenerateDocumentName(LabDocumentModel document, string orderNumber)
        {
            if (!DocumentNamePatterns.ContainsKey(document.MimeType))
            {
                throw new ArgumentException("Unsupported document media type.");
            }

            var namePattern = DocumentNamePatterns[document.MimeType];
            
            return string.Format(namePattern, orderNumber);
        }
        
        #endregion
    }
}