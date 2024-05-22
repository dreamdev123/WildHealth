using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Events.Appointments;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.AppointmentsStatistic;
using WildHealth.IntegrationEvents.AppointmentsStatistic.Payloads;

namespace WildHealth.Application.EventHandlers.Appointments;

public class CreateIntegrationEventOnAppointmentsStatisticGeneratedEvent : INotificationHandler<AppointmentsStatisticGeneratedEvent>
{
    private readonly IEventBus _eventBus;

    public CreateIntegrationEventOnAppointmentsStatisticGeneratedEvent(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(AppointmentsStatisticGeneratedEvent notification, CancellationToken cancellationToken)
    {
        var stats = notification.AppointmentsStatistic;
        var payload = new AppointmentsStatisticGeneratedPayload(
            weekStart: stats.WeekStart,
            weekEnd: stats.WeekEnd,
            visitHours: stats.VisitHours,
            visitHoursDecimal: stats.VisitHoursDecimal,
            availableHours: stats.AvailableHours,
            availableHoursDecimal: stats.AvailableHoursDecimal,
            administrativeHours: stats.AdministrativeHours,
            administrativeHoursDecimal: stats.AdministrativeHoursDecimal,
            blockedHours: stats.BlockedHours,
            blockedHoursDecimal: stats.BlockedHoursDecimal,
            booked15MinuteMeetings: stats.Booked15MinuteMeetings,
            booked25MinuteMeetings: stats.Booked25MinuteMeetings,
            booked55MinuteMeetings: stats.Booked55MinuteMeetings,
            available15MinuteMeetings: stats.Available15MinuteMeetings,
            available25MinuteMeetings: stats.Available25MinuteMeetings,
            available55MinuteMeetings: stats.Available55MinuteMeetings);

        var userMetadata = new UserMetadataModel(notification.Employee.User.UniversalId.ToString());

        var integrationEvent =
            new AppointmentsStatisticIntegrationEvent(payload, userMetadata, notification.StatisticGenerationDate);

        await _eventBus.Publish(integrationEvent, cancellationToken);
    }
}