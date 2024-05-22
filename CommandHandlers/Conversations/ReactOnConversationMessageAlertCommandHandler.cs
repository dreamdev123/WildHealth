using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.Locker;
using WildHealth.Application.CommandHandlers.Conversations.Flows;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.Models.Conversations.Alerts;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Users;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Exceptions;
using WildHealth.Common.Options;
using WildHealth.Settings;
using Newtonsoft.Json;
using MediatR;
using Polly;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class ReactOnConversationMessageAlertCommandHandler : MessagingBaseService, IRequestHandler<ReactOnConversationMessageAlertCommand>
{
    private readonly IUsersService _usersService;
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFlowMaterialization _materializeFlow;
    private readonly IOptions<PracticeOptions> _practiceOptions;
    private readonly ILocker _locker;
    
    public ReactOnConversationMessageAlertCommandHandler(
        IUsersService usersService,
        ITwilioWebClient twilioWebClient,
        ISettingsManager settingsManager, 
        IDateTimeProvider dateTimeProvider,
        IFlowMaterialization materializeFlow,
        IOptions<PracticeOptions> practiceOptions,
        ILocker locker) : base(settingsManager)
    {
        _usersService = usersService;
        _twilioWebClient = twilioWebClient;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
        _practiceOptions = practiceOptions;
        _locker = locker;
    }
    
    public async Task Handle(ReactOnConversationMessageAlertCommand command, CancellationToken cancellationToken)
    {
        var credentials = await GetMessagingCredentialsAsync(_practiceOptions.Value.WildHealth);

        _twilioWebClient.Initialize(credentials);
        
        var (message, alert) = await GetAlertAsync(
            conversationId: command.ConversationId,
            messageId: command.MessageId,
            alertId: command.AlertId
        );

        var utcNow = _dateTimeProvider.UtcNow();

        var user = await _usersService.GetByIdAsync(command.UserId);
        
        var flow = new ReactOnConversationMessageAlertFlow(
            Alert: alert,
            ActionType: command.ActionType,
            Details: command.Details,
            ReactedBy: user!,
            UtcNow: utcNow
        );

        await flow.Materialize(_materializeFlow.Materialize);

        var policy = Policy
            .Handle<AppException>(x => x.StatusCode == HttpStatusCode.Locked)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3)
            });

        await policy.ExecuteAsync(
            async () => await UpdateAlertAsync(message, alert)
        );
    }
    
    #region private

    private async Task<(ConversationMessageModel, MessageAlertModel)> GetAlertAsync(string conversationId, string messageId, string alertId)
    {
        var message = await _twilioWebClient.GetMessageAsync(
            conversationId: conversationId,
            id: messageId
        );

        if (message is null)
        {
            throw new DomainException("Alert does not exist");
        }

        var alert = message.GetAttributes().Alerts.Find(x => x.Id == alertId);
        
        if (alert is null)
        {
            throw new DomainException("Alert does not exist");
        }

        return (message, alert);
    }
    
    private async Task UpdateAlertAsync(ConversationMessageModel message, MessageAlertModel alert)
    {
        var isLocked = await _locker.LockAsync(message.Sid);

        try
        {
            var attributes = message.GetAttributes();

            attributes.Alerts.Remove(attributes.Alerts.Find(x => x.Id == alert.Id));
            
            attributes.Alerts.Add(alert);

            message.AttributesString = JsonConvert.SerializeObject(attributes);

            await _twilioWebClient.UpdateAttributes(message);
        }
        finally
        {
            if (isLocked)
            {
                await _locker.UnlockAsync(message.Sid);
            }
        }
    }
    
    #endregion
}