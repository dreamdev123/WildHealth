using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Shared.DistributedCache.Services;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Settings;
using WildHealth.Common.Options;
using Microsoft.Extensions.Options;
using MediatR;
using Newtonsoft.Json;
using Polly;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class DeleteReactionCommandHandler : MessagingBaseService, IRequestHandler<DeleteReactionCommand, Unit>
{
    private readonly ITwilioWebClient _twilioWebClient;
    private readonly IWildHealthSpecificCacheService<ConversationMessageModel, ConversationMessageModel?> _cacheService;
    private readonly IOptions<PracticeOptions> _practiceOptions;

    public DeleteReactionCommandHandler(
        ITwilioWebClient twilioWebClient,
        ISettingsManager settingsManager,
        IWildHealthSpecificCacheService<ConversationMessageModel, ConversationMessageModel?> cacheService,
        IOptions<PracticeOptions> practiceOptions): base(settingsManager)
    {
        _twilioWebClient = twilioWebClient;
        _cacheService = cacheService;
        _practiceOptions = practiceOptions;
    }
    
    public async Task<Unit> Handle(DeleteReactionCommand command, CancellationToken cancellationToken)
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
            async () => await RemoveAttributeAsync(command)
        );

        return Unit.Value;
    }
    
    #region private

    private async Task LockMessageAsync(ConversationMessageModel? message)
    {
        if (await _cacheService.GetAsync(message?.Sid) is not null)
        {
            throw new AppException(HttpStatusCode.Locked, "Message is locked by another process");
        }
        
        _cacheService.Set(message?.Sid, message);
    }
    
    private void UnlockMessage(ConversationMessageModel? message)
    {
        _cacheService.RemoveKey(message?.Sid);
    }
    
    private async Task RemoveAttributeAsync(DeleteReactionCommand command)
    {
        var message = await _twilioWebClient.GetMessageAsync(
            conversationId: command.ConversationId,
            id: command.MessageId
        );
        
        try
        {
            await LockMessageAsync(message);
            
            var attributes = message.GetAttributes();

            var reaction = attributes.Reactions.FirstOrDefault(x => x.Id == command.ReactionId);

            if (reaction is null)
            {
                return;
            }
            
            attributes.Reactions.Remove(reaction);

            message.AttributesString = JsonConvert.SerializeObject(attributes);

            await _twilioWebClient.UpdateAttributes(message);
        }
        finally
        {
            UnlockMessage(message);
        }
    }
    
    #endregion
}