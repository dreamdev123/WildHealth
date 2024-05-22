using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Twilio.Clients.Models.Conversations.Alerts;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Application.Utils.Locker;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Options;
using WildHealth.Shared.Enums;
using WildHealth.Settings;
using Newtonsoft.Json;
using MediatR;
using Polly;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class CreateMessageAlertCommandHandler : MessagingBaseService, IRequestHandler<CreateMessageAlertCommand>
{
    private readonly IDictionary<MessageAlertType, int> _expirationInMinutes = new Dictionary<MessageAlertType, int>
    {
        { MessageAlertType.TicketRequestAlert, 30 }
    };
    
    private readonly IDictionary<MessageAlertType, UserType[]> _audience = new Dictionary<MessageAlertType, UserType[]>
    {
        { MessageAlertType.TicketRequestAlert, new [] { UserType.Employee , UserType.Patient} }
    };
    
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILocker _locker;
    private readonly IOptions<PracticeOptions> _practiceOptions;
    
    public CreateMessageAlertCommandHandler(
        ITwilioWebClient twilioWebClient,
        ISettingsManager settingsManager, 
        IDateTimeProvider dateTimeProvider,
        ILocker locker,
        IOptions<PracticeOptions> practiceOptions) : base(settingsManager)
    {
        _twilioWebClient = twilioWebClient;
        _dateTimeProvider = dateTimeProvider;
        _locker = locker;
        _practiceOptions = practiceOptions;
    }
    
    public async Task Handle(CreateMessageAlertCommand command, CancellationToken cancellationToken)
    {
        var credentials = await GetMessagingCredentialsAsync(_practiceOptions.Value.WildHealth);

        _twilioWebClient.Initialize(credentials);

        var policy = Policy
            .Handle<AppException>(x => x.StatusCode == HttpStatusCode.Locked)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3)
            });

        await policy.ExecuteAsync(
            async () => await AddAttributeAsync(command)
        );
    }
    
    #region private

    private async Task AddAttributeAsync(CreateMessageAlertCommand command)
    {
        var message = await _twilioWebClient.GetMessageAsync(
            conversationId: command.ConversationId,
            id: command.MessageId
        );
        
        var isLocked = await _locker.LockAsync(message.Sid);
        var utcNow = _dateTimeProvider.UtcNow();
        
        try
        {
            var attributes = message.GetAttributes();

            var alert = new MessageAlertModel
            {
                Id = Guid.NewGuid().ToString(),
                Type = command.Type,
                CreatedAt = utcNow,
                ExpiresAt = utcNow.AddMinutes(_expirationInMinutes[command.Type]),
                Audience = _audience[command.Type].Select(x => new MessageAlertAudienceModel
                {
                    Type = x.ToString()
                }).ToList(),
                Data = command.Data
            };
        
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