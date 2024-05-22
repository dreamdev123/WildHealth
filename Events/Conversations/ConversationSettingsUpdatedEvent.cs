using MediatR;

namespace WildHealth.Application.Events.Conversations;

public record ConversationSettingsUpdatedEvent(int EmployeeId, int PreviousForwardEmployeeId,
    bool ShouldRemoveDelegatedEmployee) : INotification;
        