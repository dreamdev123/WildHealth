using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Devices;
using WildHealth.Domain.Entities.Devices;

namespace WildHealth.Application.Services.Devices
{
    public interface IDevicesService
    {
        Task CreateAsync(int userId, CreateDeviceModel model);
        Task<Device[]> GetConversationDevices(string conversationSid, Guid? excludeUniversalUserId);
    }
}
