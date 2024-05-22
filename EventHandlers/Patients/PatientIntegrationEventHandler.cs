using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.IntegrationEvents.Patients.Payloads;
using WildHealth.Application.Commands.Patients;
using WildHealth.IntegrationEvents.Patients;
using WildHealth.Infrastructure.Communication.MessageBus;
using Newtonsoft.Json;
using MediatR;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class PatientIntegrationEventHandler : IEventHandler<PatientIntegrationEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public PatientIntegrationEventHandler(
            IMediator mediator,
            ILogger<PatientIntegrationEventHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(PatientIntegrationEvent @event)
        {
            _logger.LogInformation($"Started processing patient integration event {@event.Id} with payload type: {@event.PayloadType} - {@event.Payload}");

            try
            {
                switch (@event.PayloadType)
                {
                    case nameof(PatientRegisteredPayload):
                        await ProcessPatientRegisteredPayload(
                                @event.Payload as PatientRegisteredPayload ??
                                JsonConvert.DeserializeObject<PatientRegisteredPayload>(@event.Payload.ToString()), @event);
                        break;

                    case nameof(PatientCreatedPayload):
                    case nameof(PatientLocationChangedPayload):
                    case nameof(PatientMovedPayload):
                    case nameof(PatientSubscriptionCanceledPayload):
                    case nameof(PatientUpdatedPayload):
                    case nameof(PatientRegisterFailedPayload):
                        break;

                    default: throw new ArgumentException($"Unsupported patient integration event payload type: {@event.PayloadType}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed processing patient integration event {@event.Id} with payload: {@event.Payload}. {e}");
                throw;
            }

            _logger.LogInformation($"Processed patient integration event {@event.Id} with payload: {@event.Payload}");
        }

        #region private handlers

        /// <summary>
        /// Processes patient registered payload
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        private async Task ProcessPatientRegisteredPayload(PatientRegisteredPayload payload, PatientIntegrationEvent @event)
        {
            await _mediator.Send(new PostPatientRegistrationProcessesCommand(
                practiceId: payload.PracticeId,
                patientId: payload.PatientId,
                employeeId: payload.EmployeeId,
                linkedEmployeeId: payload.LinkedEmployeeId,
                locationId: payload.LocationId,
                paymentPriceId: payload.PaymentPriceId,
                subscriptionId: payload.SubscriptionId,
                employerProductKey: payload.EmployerProductKey,
                isTrialPlan: payload.IsTrialPlan,
                addonIds: payload.AddonIds,
                founderId: payload.FounderId,
                inviteCode: payload.InviteCode,
                leadSourceId: payload.LeadSourceId,
                otherLeadSource: payload.OtherLeadSource,
                podcastSource: payload.PodcastSource,
                originatedFromEvent: @event
            ));
        }

        #endregion
    }
}