using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Messages;

namespace WildHealth.Application.Services.AdminTools.Helpers;

public interface IMessageSenderService
{
    public void Send(Message message, MyPatientsFilterModel filter);
}