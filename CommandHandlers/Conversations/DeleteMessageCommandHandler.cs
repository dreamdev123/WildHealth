using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.Locker;
using WildHealth.Common.Options;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Settings;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Twilio.Clients.Models.Conversations;
using Newtonsoft.Json;
using MediatR;
using Polly;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class DeleteMessageCommandHandler : MessagingBaseService, IRequestHandler<DeleteMessageCommand, Unit>
{
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOptions<PracticeOptions> _practiceOptions;
    private readonly IAuthTicket _authTicket;
    private readonly ILocker _locker;

    public DeleteMessageCommandHandler(
        ITwilioWebClient twilioWebClient, 
        IDateTimeProvider dateTimeProvider,
        IOptions<PracticeOptions> practiceOptions, 
        IAuthTicket authTicket,
        ILocker locker, 
        ISettingsManager settingsManager) : base(settingsManager)
    {
        _twilioWebClient = twilioWebClient;
        _dateTimeProvider = dateTimeProvider;
        _locker = locker;
        _practiceOptions = practiceOptions;
        _authTicket = authTicket;
    }
    
    public async Task<Unit> Handle(DeleteMessageCommand command, CancellationToken cancellationToken)
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
            async () => await DeleteMessage(command)
        );

        return Unit.Value;
    }
    
    #region private
    
    private async Task DeleteMessage(DeleteMessageCommand command)
    {
        var message = await _twilioWebClient.GetMessageAsync(
            conversationId: command.ConversationId,
            id: command.MessageId
        );
        
        var isLocked = await _locker.LockAsync(message.Sid);
        
        try
        {
            var attributes = message.GetAttributes();

            if (attributes.IsDeleted)
            {
                return;
            }
            
            var deletionDetails = new MessageDeletionDetails
            {
                Reason = command.Reason,
                DeletedBy = _authTicket.GetId(),
                DeletedAt = _dateTimeProvider.UtcNow()
            };

            attributes.IsDeleted = true;
            attributes.DeletionDetails = deletionDetails;
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