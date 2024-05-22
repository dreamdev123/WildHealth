using System.Threading.Tasks;
using WildHealth.Common.Models.Patients;

namespace WildHealth.Application.Services.AdminTools.Base;

public abstract class MessageServiceBase
{
    public abstract Task SendMessageAsync(int messageId, MyPatientsFilterModel filterModel);
}