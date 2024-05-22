using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Conversations;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class CreatePatientSupportConversationCommandHandler : IRequestHandler<CreatePatientSupportConversationCommand, Conversation>
    {
        private readonly IMessagingConversationService _messagingConversationService;
        private readonly IConversationsService _conversationsService;
        private readonly IPatientsService _patientsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CreatePatientSupportConversationCommandHandler(
            IMessagingConversationService messagingConversationService,
            IConversationsService conversationsService,
            IPatientsService patientsService,
            ITransactionManager transactionManager,
            IMediator mediator,
            ILogger<CreatePatientSupportConversationCommandHandler> logger)
        {
            _messagingConversationService = messagingConversationService;
            _conversationsService = conversationsService;
            _patientsService = patientsService;
            _transactionManager = transactionManager;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Conversation> Handle(CreatePatientSupportConversationCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating support conversation for patient with id {command.PatientId} has been started.");

            var patient = await _patientsService.GetByIdAsync(command.PatientId);

            var conversation = ConversationDomain.CreateConversation(
                subject: command.Subject,
                locationId: command.LocationId,
                startDate: DateTime.UtcNow,
                patients: new [] { patient },
                type: ConversationType.Support,
                vendor: ConversationVendorType.Twilio
            );

            await using var transaction = _transactionManager.BeginTransaction();

            try
            {
                conversation = await _conversationsService.CreateConversationAsync(conversation);

                conversation = await LinkConversationToVendor(command.PracticeId, conversation);

                await LinkConversationParticipants(command.PracticeId, conversation);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError($"Creating support conversation for patient with id {command.PatientId} has been failed. {e.ToString()}");

                await transaction.RollbackAsync(cancellationToken);

                throw;
            }

            _logger.LogInformation($"Creating support conversation for patient with id {command.PatientId} has been finished.");

            var conversationCreatedEvent = new ConversationCreatedEvent(conversation, UserType.Patient);

            await _mediator.Publish(conversationCreatedEvent, cancellationToken);

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
            var conversationDomain = ConversationDomain.Create(conversation);
            var vendorConversation = await _messagingConversationService.CreateConversationAsync(practiceId, conversation);

            conversationDomain.SetVendorExternalId(vendorConversation.Sid);

            await _conversationsService.UpdateConversationAsync(conversation);

            return conversation;
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

        #endregion
    }
}