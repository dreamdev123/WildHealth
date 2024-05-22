using WildHealth.Application.Services.AdminTools.Base;
using WildHealth.Domain.Enums.Messages;

namespace WildHealth.Application.Utils.AdminTools;

public static class ServiceRegistrationHelper
{
    public delegate MessageServiceBase ServiceResolver(MessageType type);
}