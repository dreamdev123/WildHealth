using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Domain.AppointmentReminder.Bot;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Chatbots;
using WildHealth.Domain.Exceptions;
using WildHealth.IntegrationEvents.SMS.Payloads;

namespace WildHealth.Application.Domain.AppointmentReminder;

public record ChatbotMessageReceivedEvent(SMSMessagingSource SmsMessagingSource, string From, string To, string Body) : INotification;

public class ChatbotMessageReceivedEventHandler : INotificationHandler<ChatbotMessageReceivedEvent>
{
    private readonly IAppointmentReminderService _appointmentReminderService;
    private readonly IUsersService _usersService;
    private readonly IPatientProfileService _patientProfileService;
    private readonly IFlowMaterialization _materializer;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChatbotMessageReceivedEventHandler(
        IAppointmentReminderService appointmentReminderService, 
        IUsersService usersService, 
        IFlowMaterialization materializer, 
        IPatientProfileService patientProfileService, 
        IDateTimeProvider dateTimeProvider)
    {
        _appointmentReminderService = appointmentReminderService;
        _usersService = usersService;
        _materializer = materializer;
        _patientProfileService = patientProfileService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(ChatbotMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        var chatBotType = notification.SmsMessagingSource switch
        {
            SMSMessagingSource.AppointmentReminder => ChatbotType.AppointmentReminder,
            _ => throw new DomainException($"Unknown source {notification.SmsMessagingSource}")
        };

        var user = (await _usersService.GetByPhoneAsync(notification.From.Trim())).FirstOrDefault();
        if (chatBotType == ChatbotType.AppointmentReminder && user is not null)
        {
            var activeReminderBots = await _appointmentReminderService.GetAllAsync(user.Patient.GetId(), ChatbotType.AppointmentReminder);
            var dashboardLink = await _patientProfileService.GetDashboardLink(user.PracticeId);
            var botPool = new AppointmentReminderBotPool(activeReminderBots, 48, _dateTimeProvider.UtcNow(), dashboardLink);
            var bot = botPool.Find(notification.Body);
            await _materializer.Materialize(bot.Tell(notification.Body));
        }
    }
}
