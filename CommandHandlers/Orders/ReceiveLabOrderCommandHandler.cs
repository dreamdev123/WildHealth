using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Domain.Entities.Orders;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.ClarityCore.WebClients.Labs;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.ClarityCore.Models.Labs;
using WildHealth.Domain.Enums.AddOns;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class ReceiveLabOrderCommandHandler : IRequestHandler<ReceiveLabOrderCommand, LabOrder>
    {
        private static readonly IDictionary<AddOnProvider, string> DrawingFeeMap = new Dictionary<AddOnProvider, string>
        {
            { AddOnProvider.LabCorp, "36415" }
        };
        
        private readonly ILabOrdersService _labOrdersService;
        private readonly IPatientsService _patientsService;
        private readonly IAddOnsService _addOnsService;
        private readonly ILabsWebClient _webClient;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public ReceiveLabOrderCommandHandler(
            ILabOrdersService labOrdersService,
            IPatientsService patientsService,
            IAddOnsService addOnsService,
            ILabsWebClient webClient,
            IMediator mediator,
            ILogger<ReceiveLabOrderCommandHandler> logger)
        {
            _labOrdersService = labOrdersService;
            _patientsService = patientsService;
            _addOnsService = addOnsService;
            _webClient = webClient;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<LabOrder> Handle(ReceiveLabOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Receiving of Lab order with number: {command.OrderNumber} has been started.");

            var patientId = command.PatientId;

            var patient = await _patientsService.GetByIdAsync(command.PatientId);

            var reportId = command.ReportId;

            var integrationIds = command.TestCodes;

            var report = await GetLabReportAsync(
                patientId: patientId,
                reportId: reportId
            );

            await AssertOrderDoesNotExistAsync(patientId, report.OrderNumber);

            var addOns = await FetchAddOnsAsync(integrationIds, patient);

            var order = await CreateOrderAsync(patientId, report.OrderNumber, addOns);

            await _mediator.Send(new SendLabOrderRequisitionEmailCommand(order), cancellationToken);

            await _mediator.Send(new SendLabReminderEmailCommand(order.GetId()), cancellationToken);

            _logger.LogInformation($"Receiving of Lab order with number: {command.OrderNumber} has been finished.");

            return order;
        }

        #region private

        /// <summary>
        /// Asserts order with the same order number does not exist
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task AssertOrderDoesNotExistAsync(int patientId, string orderNumber)
        {
            try
            {
                var order = await _labOrdersService.GetByNumberAsync(orderNumber, patientId);

                if (!(order is null))
                {
                    throw new AppException(HttpStatusCode.BadRequest, $"Order with number: {orderNumber} already exists.");
                }
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Ignore this exception as expected result
            }
        }

        /// <summary>
        /// Fetches and returns add-ons by integration ids
        /// </summary>
        /// <param name="integrationIds"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task<AddOn[]> FetchAddOnsAsync(string[] integrationIds, Patient patient)
        {
            var employerKey = patient?.CurrentSubscription?.EmployerProduct?.Key!;
            
            var addOns = (await _addOnsService.GetByIntegrationIdsAsync(integrationIds, employerKey)).ToArray();
            
            var provider = addOns.First().Provider;

            if (!DrawingFeeMap.ContainsKey(provider))
            {
                return addOns;
            }
            
            var drawingFeeIntegrationId = DrawingFeeMap[provider];
            var drawingFeeAddOns = await _addOnsService.GetByIntegrationIdsAsync(new[] { drawingFeeIntegrationId }, employerKey);
            return addOns.Concat(drawingFeeAddOns).ToArray();
        }

        /// <summary>
        /// Creates and returns Lab order
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="orderNumber"></param>
        /// <param name="addOns"></param>
        /// <returns></returns>
        private Task<LabOrder> CreateOrderAsync(int patientId, string orderNumber, AddOn[] addOns)
        {
            var addOnIds = addOns.Select(x => x.GetId()).ToArray();
            
            var command = new CreateLabOrderCommand(
                patientId: patientId,
                orderNumber: orderNumber,
                addOnIds: addOnIds
            );

            return _mediator.Send(command);
        }


        /// <summary>
        /// Fetch and returns HL7 document
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<LabReportModel> GetLabReportAsync(int patientId, int reportId)
        {
            var report = await _webClient.GetPatientLabReportAsync(patientId.ToString(), reportId);

            if (report is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Lab report with id ${reportId} does not exist.");
            }

            return report;
        }

        #endregion
    }
}