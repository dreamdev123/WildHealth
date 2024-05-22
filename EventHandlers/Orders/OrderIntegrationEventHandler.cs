using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Orders;
using WildHealth.IntegrationEvents.Orders.Payloads;
using WildHealth.IntegrationEvents._Base;
using WildHealth.Application.Events.Orders;
using Newtonsoft.Json;
using MediatR;

namespace WildHealth.Application.EventHandlers.Orders
{
    public class OrderIntegrationEventHandler : IEventHandler<OrderIntegrationEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public OrderIntegrationEventHandler(
            IMediator mediator, 
            ILogger<OrderIntegrationEventHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(OrderIntegrationEvent @event)
        {
            _logger.LogInformation($"Started processing order integration event {@event.Id} with payload type: {@event.PayloadType} - {@event.Payload}");

            try
            {
                switch (@event.PayloadType)
                {
                    case nameof(LabOrderPlacedPayload): 
                        await ProcessLabOrderPlacedPayload(JsonConvert.DeserializeObject<LabOrderPlacedPayload>(@event.Payload.ToString()), @event.Patient); 
                        break;
                
                    case nameof(LabOrderCompletedPayload): 
                        await ProcessLabOrderCompletedPayload(JsonConvert.DeserializeObject<LabOrderCompletedPayload>(@event.Payload.ToString()), @event.Patient); 
                        break;
                    
                    case nameof(LabOrderCorrectedPayload): 
                        await ProcessOrderCorrectedPayload(JsonConvert.DeserializeObject<LabOrderCorrectedPayload>(@event.Payload.ToString()), @event.Patient); 
                        break;

                    case nameof(LabOrderUnsolicitedPayload):
                        await ProcessOrderUnsolicitedPayload(JsonConvert.DeserializeObject<LabOrderUnsolicitedPayload>(@event.Payload.ToString()), @event.Patient);
                        break;

                    case nameof(LabOrderFinalizedPayload):
                        _logger.LogInformation("Ignoring action on LabOrderFinalizedPayload");
                        break;
                
                    default: throw new ArgumentException("Unsupported order integration event payload");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed processing order integration event {@event.Id} with payload: {@event.Payload}. {e}");
                throw;
            }
            
            _logger.LogInformation($"Processed order integration event {@event.Id} with payload: {@event.Payload}");
        }
        
        #region private handlers
        
        /// <summary>
        /// Processes lab order placed payload
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ProcessLabOrderPlacedPayload(LabOrderPlacedPayload payload, PatientMetadataModel patient)
        {
            await _mediator.Publish(new LabOrderPlacedEvent(
                patientId: patient.Id,
                reportId: payload.ReportId,
                orderNumber: payload.OrderNumber,
                testCodes: payload.TestCodes
            ));
        }
        
        /// <summary>
        /// Processes lab order completed payload
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ProcessLabOrderCompletedPayload(LabOrderCompletedPayload payload, PatientMetadataModel patient)
        {
            await _mediator.Publish(new LabOrderCompletedEvent(
                patientId: patient.Id,
                reportId: payload.ReportId,
                orderNumber: payload.OrderNumber
            ));
        }

        /// <summary>
        /// Process lab order corrected payload
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ProcessOrderCorrectedPayload(LabOrderCorrectedPayload payload, PatientMetadataModel patient)
        {
            await _mediator.Publish(new LabOrderCorrectedEvent(
                patientId: patient.Id,
                reportId: payload.ReportId,
                orderNumber: payload.OrderNumber
            ));
        }

        /// <summary>
        /// Process lab order unsolicited payload
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ProcessOrderUnsolicitedPayload(LabOrderUnsolicitedPayload payload, PatientMetadataModel patient)
        {
            await _mediator.Publish(new LabOrderUnsolicitedEvent(
                patientId: patient.Id,
                reportId: payload.ReportId,
                orderNumber: payload.OrderNumber
            ));
        }
        
        #endregion
    }
}