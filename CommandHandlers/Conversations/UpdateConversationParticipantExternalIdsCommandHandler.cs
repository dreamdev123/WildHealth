using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class UpdateConversationParticipantExternalIdsCommandHandler : MessagingBaseService, IRequestHandler<UpdateConversationParticipantExternalIdsCommand>
    {
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IConversationParticipantMessageSentIndexService _conversationParticipantMessageSentIndexService;
        private readonly IConversationsService _conversationsService;
        private readonly IConversationParticipantPatientService _conversationParticipantPatientService;
        private readonly IConversationParticipantEmployeeService _conversationParticipantEmployeeService;
        private readonly IGeneralRepository<ConversationParticipantEmployee> _conversationParticipantEmployeeRepository;
        private readonly IGeneralRepository<ConversationParticipantPatient> _conversationParticipantPatientRepository;
        private readonly ILogger _logger;

        public UpdateConversationParticipantExternalIdsCommandHandler(
            ITwilioWebClient twilioWebClient,
            IConversationParticipantMessageSentIndexService conversationParticipantMessageSentIndexService,
            IConversationParticipantPatientService conversationParticipantPatientService,
            IConversationParticipantEmployeeService conversationParticipantEmployeeService,
            IGeneralRepository<ConversationParticipantEmployee> conversationParticipantEmployeeRepository,
            IGeneralRepository<ConversationParticipantPatient> conversationParticipantPatientRepository,
            IConversationsService conversationsService,
            ISettingsManager settingsManager,
            ILogger<UpdateAllConversationParticipantSentIndexesCommandHandler> logger) : base(settingsManager)
        {
            _twilioWebClient = twilioWebClient;
            _conversationParticipantMessageSentIndexService = conversationParticipantMessageSentIndexService;
            _conversationParticipantPatientService = conversationParticipantPatientService;
            _conversationParticipantEmployeeService = conversationParticipantEmployeeService;
            _conversationParticipantEmployeeRepository = conversationParticipantEmployeeRepository;
            _conversationParticipantPatientRepository = conversationParticipantPatientRepository;
            _conversationsService = conversationsService;
            _logger = logger;
        }

        public async Task Handle(UpdateConversationParticipantExternalIdsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Started updating external ids for participants whose are missing has started");

            var patientParticipants = 
                await _conversationParticipantPatientService.GetHealthParticipantsWithoutExternalId();

            var employeeParticipants =
                await _conversationParticipantEmployeeService.GetHealthParticipantsWithoutExternalId();

            foreach (var patientParticipant in patientParticipants)
            {
                _logger.LogInformation($"Starting [ParticipantIdentity] = {patientParticipant.Patient.User.MessagingIdentity()}, [ConversationId] = {patientParticipant.Conversation.VendorExternalId}");
                
                try
                {
                    var conversation = patientParticipant.Conversation;

                    var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

                    _twilioWebClient.Initialize(credentials);

                    var remoteParticipantsResponse =
                        await _twilioWebClient.GetConversationParticipantResourcesAsync(conversation.VendorExternalId);

                    var remoteParticipant = remoteParticipantsResponse.Participants
                        .FirstOrDefault(o => o.Identity == patientParticipant.Patient.User.MessagingIdentity());

                    if (remoteParticipant is not null)
                    {
                        patientParticipant.SetVendorExternalId(remoteParticipant.Sid);
                        patientParticipant.SetVendorExternalIdentity(remoteParticipant.Identity);

                        _conversationParticipantPatientRepository.Edit(patientParticipant);

                        await _conversationParticipantPatientRepository.SaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Failed [ParticipantIdentity] = {patientParticipant.Patient.User.MessagingIdentity()}, [ConversationId] = {patientParticipant.Conversation.VendorExternalId} - {ex}");
                }
            }
            
            foreach (var employeeParticipant in employeeParticipants)
            {
                _logger.LogInformation($"Starting [EmployeeIdentity] = {employeeParticipant.Employee.User.MessagingIdentity()}, [ConversationId] = {employeeParticipant.Conversation.VendorExternalId}");

                try
                {
                    var conversation = employeeParticipant.Conversation;

                    var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);

                    _twilioWebClient.Initialize(credentials);

                    var remoteParticipantsResponse =
                        await _twilioWebClient.GetConversationParticipantResourcesAsync(conversation.VendorExternalId);

                    var remoteParticipant = remoteParticipantsResponse.Participants
                        .FirstOrDefault(o => o.Identity == employeeParticipant.Employee.User.MessagingIdentity());

                    if (remoteParticipant is not null)
                    {
                        employeeParticipant.SetVendorExternalId(remoteParticipant.Sid);
                        employeeParticipant.SetVendorExternalIdentity(remoteParticipant.Identity);

                        _conversationParticipantEmployeeRepository.Edit(employeeParticipant);

                        await _conversationParticipantEmployeeRepository.SaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Failed [ParticipantIdentity] = {employeeParticipant.Employee.User.MessagingIdentity()}, [ConversationId] = {employeeParticipant.Conversation.VendorExternalId} - {ex}");
                }
                
            }
            
            _logger.LogInformation($"Started updating external ids for participants whose are missing has finished");
        }
    }
}