using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class SendUnreadMessagesPremiumSmsReminderCommandHandler : IRequestHandler<SendUnreadMessagesParticularPatientsSmsReminderCommand>
{
    private readonly IConversationParticipantMessageReadIndexService _conversationParticipantMessageReadIndexService;
    private readonly INotificationService _notificationService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly IEmployeeService _employeeService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;

    public SendUnreadMessagesPremiumSmsReminderCommandHandler(
        IConversationParticipantMessageReadIndexService conversationParticipantMessageReadIndexService, 
        INotificationService notificationService, 
        IFeatureFlagsService featureFlagsService, 
        IEmployeeService employeeService, 
        IDateTimeProvider dateTimeProvider, 
        ILogger<SendUnreadMessagesPremiumSmsReminderCommandHandler> logger)
    {
        _conversationParticipantMessageReadIndexService = conversationParticipantMessageReadIndexService;
        _notificationService = notificationService;
        _featureFlagsService = featureFlagsService;
        _employeeService = employeeService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task Handle(SendUnreadMessagesParticularPatientsSmsReminderCommand request, CancellationToken cancellationToken)
    {
        var checkTime = _dateTimeProvider.UtcNow();

        if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.ConversationsBackgroundJobs))
        {
            _logger.LogInformation("[SendUnreadMessagesPremiumSmsReminderCommand] Feature flag disabled: [WH-All-Conversations-BackgroundJobs]");
            return;
        }

        var results = await _conversationParticipantMessageReadIndexService.GetUnreadMessagesFromParticularPatientAsync();

        if (!results.Any())
        {
            _logger.LogInformation("[SendUnreadMessagesPremiumSmsReminderCommand] No messages to send at this time");
            return;
        }
        
        var employeeIds = results.Select(x => x.EmployeeId).ToArray();

        var specification = EmployeeSpecifications.WithUser;
        
        var employees = await _employeeService.GetByIdsAsync(employeeIds, specification);

        foreach (var result in results)
        {
            var employee = employees.FirstOrDefault(x => x.Id == result.EmployeeId);

            if (employee is null)
            {
                _logger.LogWarning("[SendUnreadMessagesPremiumSmsReminderCommand] No employee with id {employeeId}", result.EmployeeId);
                
                continue;
            }
            
            var notification = new UnreadMessagesFromParticularPatientsNotification(
                employee: employee,
                unreadMessagesCount: result.UnreadMessages,
                patientsCount: result.PatientCount,
                now: checkTime
            );

            await _notificationService.CreateNotificationAsync(notification);
        }
    }
}