using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Conversations;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Enums;
using MediatR;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Conversation;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class CreatePatientHealthCareConversationCommandHandler : IRequestHandler<CreatePatientHealthCareConversationCommand, Conversation>
    {
        private readonly IConversationsService _conversationsService;
        private readonly IPatientsService _patientsService;
        private readonly IMessagingConversationService _messagingConversationService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CreatePatientHealthCareConversationCommandHandler(
            IConversationsService conversationsService,
            IPatientsService patientsService,
            IMessagingConversationService messagingConversationService,
            IMediator mediator,
            ILogger<CreatePatientHealthCareConversationCommandHandler> logger)
        {
            _conversationsService = conversationsService;
            _patientsService = patientsService;
            _messagingConversationService = messagingConversationService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Conversation> Handle(CreatePatientHealthCareConversationCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating health care conversation for patient with id {command.PatientId} has been started.");
            var spec = PatientSpecifications.CreateConversationWithPatientSpecification;
            var patient = await _patientsService.GetByIdAsync(command.PatientId,spec);

            if (!command.ActiveEmployees.Any())
            {
                throw new AppException(HttpStatusCode.BadRequest, $"There are not any active employees to add into conversation.");
            }

            var conversation = await TryGetHealthCareConversationAsync(command.PatientId);

            if (conversation is not null)
            {
                var conversationDomain = ConversationDomain.Create(conversation);
                conversationDomain.AddEmployeeParticipants(
                    employees: command.ActiveEmployees,
                    isActive: true
                );

                if (command.InactiveEmployees?.Any() ?? false)
                {
                    conversationDomain.AddEmployeeParticipants(
                        employees: command.InactiveEmployees,
                        isActive: false
                    );
                }

                if (command.DelegatedEmployees?.Any() ?? false)
                {
                    conversationDomain.AddDelegatedEmployeeParticipants(command.DelegatedEmployees);
                }

                await _conversationsService.UpdateConversationAsync(conversation);

                return conversation;
            }

            conversation = ConversationDomain.CreateConversation(
                subject: $"Health Team conversation for {patient.User.GetFullname()}",
                locationId: command.LocationId,
                startDate: DateTime.UtcNow,
                activeEmployees: command.ActiveEmployees,
                inactiveEmployees: command.InactiveEmployees,
                delegatedEmployees: command.DelegatedEmployees,
                patients: new [] { patient },
                type: ConversationType.HealthCare,
                vendor: ConversationVendorType.Twilio
            );

            conversation = await _conversationsService.CreateConversationAsync(conversation);

            conversation = await LinkConversationToVendor(command.PracticeId, conversation);

            await LinkConversationParticipants(command.PracticeId, conversation);

            var conversationCreatedEvent = new ConversationCreatedEvent(conversation, UserType.Employee);

            await _mediator.Publish(conversationCreatedEvent, cancellationToken);

            _logger.LogInformation($"Creating health care conversation for patient with id {command.PatientId} has been finished.");

            return conversation;
        }

        #region private

        /// <summary>
        /// Link conversations
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="conversation"></param>
        /// <returns></returns>
        private async Task<Conversation> LinkConversationToVendor(int practiceId, Conversation conversation)
        {
            try
            {
                _logger.LogInformation($"Linking conversation with vendor for [conversationId]: {conversation.Id}");
                var vendorConversation =
                    await _messagingConversationService.CreateConversationAsync(practiceId, conversation);

                var conversationDomain = ConversationDomain.Create(conversation);

                conversationDomain.SetVendorExternalId(vendorConversation.Sid);

                // possible point of failure: at this point conversationVendorId should be set.
                return await _conversationsService.UpdateConversationAsync(conversation);
                
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Error updating conversation with error {e}");
             
                throw new ApplicationException($"Error updating conversation with error {e}");
            }
        }

        /// <summary>
        /// Links conversation participants
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="conversation"></param>
        /// <returns></returns>
        private async Task LinkConversationParticipants(int practiceId, Conversation conversation)
        {
            foreach (var participant in conversation.PatientParticipants)
            {
                var vendorConversation = await _messagingConversationService.CreateConversationParticipantAsync(
                    practiceId: practiceId, 
                    conversation: conversation,
                    messagingIdentity: participant.Patient.User.MessagingIdentity(),
                    name: $"{participant.Patient.User.FirstName} {participant.Patient.User.LastName}"
                );

                participant.SetVendorExternalId(vendorConversation.Sid);
                participant.SetVendorExternalIdentity(vendorConversation.Identity);
            }

            foreach (var participant in conversation.EmployeeParticipants)
            {
                var vendorConversation = await _messagingConversationService.CreateConversationParticipantAsync(
                    practiceId: practiceId, 
                    conversation: conversation,
                    messagingIdentity: participant.Employee.User.MessagingIdentity(),
                    name: $"{participant.Employee.User.FirstName} {participant.Employee.User.LastName}"
                );

                participant.AddVendorExternalId(new ConversationParticipantEmployeeIntegration(
                    participant,
                    IntegrationVendor.Twilio,
                    IntegrationPurposes.Patient.ExternalId, 
                    vendorConversation.Sid));
                
                participant.SetVendorExternalId(vendorConversation.Sid);
                participant.SetVendorExternalIdentity(vendorConversation.Identity);
            }

            await _conversationsService.UpdateConversationAsync(conversation);
        }

        /// <summary>
        /// Try to get patient health care conversation if it is exist
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        private async Task<Conversation?> TryGetHealthCareConversationAsync(int patientId)
        {
            try
            {
                return await _conversationsService.GetHealthConversationByPatientAsync(patientId);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error creating health care conversation for [PatientId] = {patientId} with error: {ex.ToString()}");

                return null;
            }
        }

        #endregion
    }
}
