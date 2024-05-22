using WildHealth.Application.Services.AdminTools.Base;
using WildHealth.Application.Utils.AdminTools;
using WildHealth.BackgroundJobs;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Messages;
using WildHealth.Domain.Enums.Messages;

namespace WildHealth.Application.Services.AdminTools.Helpers;

public class MessageSenderService: IMessageSenderService
{
    private readonly MessageServiceBase _emailServiceBase;
    private readonly MessageServiceBase _clarityServiceBase;
    private readonly MessageServiceBase _smsServiceBase;
    private readonly MessageServiceBase _mobileServiceBase;
    private readonly IBackgroundJobsService _backgroundJobsService;

    public MessageSenderService(
        IBackgroundJobsService backgroundJobsService,
        ServiceRegistrationHelper.ServiceResolver serviceResolver)
    {
        _mobileServiceBase = serviceResolver(MessageType.MobileMessage);
        _emailServiceBase = serviceResolver(MessageType.Email);
        _clarityServiceBase = serviceResolver(MessageType.ClarityMessage);
        _smsServiceBase = serviceResolver(MessageType.Sms);
        _backgroundJobsService = backgroundJobsService;
    }

    public void Send(Message message, MyPatientsFilterModel filter)
    {
        switch (message.Type)
        {
            case MessageType.Email:
                _backgroundJobsService.Enqueue(() => _emailServiceBase.SendMessageAsync(message.GetId(), filter));
                break;
            case MessageType.Sms:
                _backgroundJobsService.Enqueue(() =>  _smsServiceBase.SendMessageAsync(message.GetId(), filter));
                break;
            case MessageType.ClarityMessage:
                _backgroundJobsService.Enqueue(() => _clarityServiceBase.SendMessageAsync(message.GetId(), filter));
                break;
            case MessageType.MobileMessage:
                _backgroundJobsService.Enqueue(() => _mobileServiceBase.SendMessageAsync(message.GetId(), filter));
                break;
        }   
    }
}