using MediatR;

namespace WildHealth.Application.Events.Ai;

public record AiConversationMessageAddedEvent(string ConversationSid, string MessageSid, string UserUniversalId) : INotification;
