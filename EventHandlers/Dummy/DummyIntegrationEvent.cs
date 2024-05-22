using WildHealth.IntegrationEvents._Base;

namespace WildHealth.Application.EventHandlers.Dummy;

public class DummyIntegrationEvent : BaseIntegrationEvent
{
    public int SleepSeconds { get; set; }
}