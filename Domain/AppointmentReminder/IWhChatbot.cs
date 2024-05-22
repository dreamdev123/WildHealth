using WildHealth.Application.Functional.Flow;

namespace WildHealth.Application.Domain.AppointmentReminder;

public interface IWhChatbot
{
    MaterialisableFlowResult Tell(string? inputMessage = null);
}