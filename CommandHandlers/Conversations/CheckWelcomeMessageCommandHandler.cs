using System;
using System.Threading;
using HandlebarsDotNet;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Conversations;
using WildHealth.Application.Services.Patients;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Settings;
using MediatR;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Models.Conversation;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class CheckWelcomeMessageCommandHandler : MessagingBaseService, IRequestHandler<SendWelcomeMessageToNewPatientsCommand>
    {
        private readonly IConversationsService _conversationsService;
        private readonly IMessagingConversationService _messagingConversationService;
        private readonly IConversationParticipantPatientService _conversationParticipantPatientsService;
        private readonly IPatientsService _patientsService;
        private readonly IMediator _mediator;
        private readonly ITwilioWebClient _twilioWebClient;

        private readonly ILogger _logger;
        
        public CheckWelcomeMessageCommandHandler(
         
            IConversationsService conversationsService,
            IConversationParticipantPatientService conversationParticipantPatientsService,
            IMessagingConversationService messagingConversationService,
            ISettingsManager settingsManager,
            IPatientsService patientsService,
            ITwilioWebClient twilioWebClient,
            IMediator mediator,
            ILogger<CheckWelcomeMessageCommandHandler> logger) : base(settingsManager)
        {
            _mediator = mediator;
            _conversationsService = conversationsService;
            _conversationParticipantPatientsService = conversationParticipantPatientsService;
            _messagingConversationService = messagingConversationService;
            _patientsService = patientsService;
            _twilioWebClient = twilioWebClient;
            _logger = logger;
    
        }

        public async Task Handle(SendWelcomeMessageToNewPatientsCommand request, CancellationToken cancellationToken)
        {
            // Steps to send a welcome message
            // check new patients without any started conversation
            // patient has at least 1 day since registration
            // patient must have an assigned health coach
            // health coach must not have the role of "fellow"

            _logger.LogInformation($"[CheckWelcomeMessageCommandHandler] handle get called");
           
            var patientsWoMessages = await _patientsService.GetPatientsWOMessagesOrConversation();
            
            foreach(var patient in patientsWoMessages)
            {
                try
                {
                    if(!patient.RegistrationDate.HasValue) continue;
                    
                    var signupDate = patient.RegistrationDate.Value;
                    var timePastSinceSignup = DateTime.UtcNow.Subtract(signupDate);
                    var hasLessThanOneDayRegistered = timePastSinceSignup.TotalDays < 1 ;
                    var healthCoach = patient.GetHealthCoach();
                    var isExperiencedPatient = timePastSinceSignup.TotalDays > 30;
                    if (healthCoach is null || hasLessThanOneDayRegistered || healthCoach.RoleId == Roles.FellowId || isExperiencedPatient)
                    {
                        _logger.LogInformation($"[CheckWelcomeMessageCommandHandler] Patient [ID] : {patient.Id} skipped by unaccomplished condition");
                        continue;
                    }
                    
                    await CreateWelcomePatientHealthCareConversation(request, patient, DateTime.UtcNow);
                }
                catch (Exception e)
                {
                    // ignore
                    _logger.LogWarning($"Send Welcome Message to patient with [Id] = {patient.GetId()} has failed with [Error]: {e.ToString()}");
                }
            }
        }
        
        #region private
        
        private async Task<Conversation?> CreateWelcomePatientHealthCareConversation(SendWelcomeMessageToNewPatientsCommand command, Patient patient, DateTime time)
        {   
           var conversation = await _mediator.Send(new StartHealthCareConversationCommand(
                employeeId: patient.GetHealthCoach().GetId(),
                patientId:  patient.GetId(),
                practiceId: patient.User.PracticeId,
                locationId: patient.LocationId
            ));

           if (conversation is null) return null;

           var model = new CreateConversationMessageModel
           {
               ConversationSid = conversation.VendorExternalId,
               Author = patient.GetHealthCoach().User.MessagingIdentity(),
               Body = ParseMessage(command.Message, patient)
           };
            
           var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);
           _twilioWebClient.Initialize(credentials);
           await _twilioWebClient.CreateConversationMessageAsync(model);
           
           await UpdateConversation(conversation);
           
           _logger.LogInformation($"[CheckWelcomeMessageCommandHandler] Sending Welcome message to {patient.Id} has been finished.");
           
           return conversation;
        }
        
        private async Task UpdateConversation(Conversation conversation)
        {
            var trackedConversation = await _conversationsService.GetByIdAsync(conversation.GetId());
            var trackedConversationDomain = ConversationDomain.Create(trackedConversation);
            trackedConversationDomain.SetHasMessages(true);
            await _conversationsService.UpdateConversationAsync(trackedConversation);
        }

       
        private string ParseMessage(string templateSource, Patient patient){

            var template = Handlebars.Compile(templateSource);
            
            var data = new {
                patient_first_name = patient.User.FirstName,
                coach_first_name = patient.GetHealthCoach().User.FirstName
            };

            return template(data);
        }

        #endregion
    }
}