using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Messaging.Conversations;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Shared.Enums;
using MediatR;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.Events.Ai;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class StartPatientPlaygroundConversationCommandHandler : IRequestHandler<StartPatientPlaygroundConversationCommand, Conversation>
{
    private readonly IMessagingConversationService _messagingConversationService;
    private readonly IConversationsService _conversationsService;
    private readonly IEmployeeService _employeesService;
    private readonly IPatientsService _patientsService;
    private readonly IDurableMediator _durableMediator;
    private readonly IMediator _mediator;
    private const int MaxSubjectLength = 256;
    
    public StartPatientPlaygroundConversationCommandHandler(
        IMessagingConversationService messagingConversationService,
        IConversationsService conversationsService,
        IEmployeeService employeesService,
        IPatientsService patientsService,
        IDurableMediator durableMediator,
        IMediator mediator)
    {
        _messagingConversationService = messagingConversationService;
        _conversationsService = conversationsService;
        _employeesService = employeesService;
        _patientsService = patientsService;
        _durableMediator = durableMediator;
        _mediator = mediator;
    }
    
    public async Task<Conversation> Handle(StartPatientPlaygroundConversationCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeesService.GetByIdAsync(command.EmployeeId);

        var patient = await _patientsService.GetByIdAsync(command.PatientId);

        var subject = command.Prompt;
        var conversation = ConversationDomain.CreateConversation(
            subject: subject.Substring(0, Math.Min(MaxSubjectLength, subject.Length)).Trim(),
            startDate: DateTime.UtcNow,
            locationId: command.LocationId,
            owner: employee,
            activeEmployees: Array.Empty<Employee>(),
            inactiveEmployees: Array.Empty<Employee>(),
            delegatedEmployees: Array.Empty<(Employee employee, Employee delegatedBy)>(),
            patients: new[] { patient },
            type: ConversationType.PatientPlayground,
            vendor: ConversationVendorType.Twilio
        );

        conversation = await _conversationsService.CreateConversationAsync(conversation);

        conversation = await LinkConversationToVendor(command.PracticeId, conversation);

        await LinkConversationParticipants(command.PracticeId, conversation);

        var conversationCreatedEvent = new ConversationCreatedEvent(conversation, UserType.Employee);

        await _mediator.Publish(conversationCreatedEvent, cancellationToken);
        
        await SendMessageAsync(conversation, employee, command.Prompt);

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
        
    private async Task SendMessageAsync(Conversation conversation, Employee author, string body) 
    {
        var conversationDomain = ConversationDomain.Create(conversation);
    
        var externalIdentity = conversationDomain.GetEmployeeParticipantExternalIdentity(author.GetId());

        if (string.IsNullOrEmpty(externalIdentity)) return;
    
        var sendMessageCommand = new SendMessageCommand(conversation, externalIdentity, body, null);

        var message = await _mediator.Send(sendMessageCommand);
        
        var aiEvent = new AiConversationMessageAddedEvent(message.ConversationSid, message.Sid, author.User.UniversalId.ToString());
        
        await _durableMediator.Publish(aiEvent);
    }

    #endregion
}