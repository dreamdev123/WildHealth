using WildHealth.Application.Functional.Flow;

namespace WildHealth.Application.Domain.AppointmentReminder.Bot;

public record NoneAppointmentReminderBot : IWhChatbot
{
    public MaterialisableFlowResult Tell(string? inputMessage = null) => MaterialisableFlowResult.Empty;
}