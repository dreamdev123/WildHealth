using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Devices;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Devices;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Devices
{
    public class DevicesService : IDevicesService
    {
        private readonly IGeneralRepository<Device> _devicesRepository;
        private readonly IGeneralRepository<Conversation> _conversationsRepository;

        public DevicesService(IGeneralRepository<Device> devicesRepository, 
            IGeneralRepository<Conversation> conversationsRepository)
        {
            _devicesRepository = devicesRepository;
            _conversationsRepository = conversationsRepository;
        }

        public async Task CreateAsync(int userId, CreateDeviceModel model)
        {
            if (await _devicesRepository.NotExistsAsync(x => x.UserId == userId && x.DeviceToken == model.DeviceToken))
            {
                var device = new Device
                {
                    UserId = userId,
                    DeviceToken = model.DeviceToken
                };

                await _devicesRepository.AddAsync(device);
                await _devicesRepository.SaveAsync();
            }
        }

        public async Task<Device[]> GetConversationDevices(string conversationSid, Guid? excludeUniversalUserId)
        {
            var devices = await _conversationsRepository
              .All()
              .ByVendorExternalId(conversationSid)
              .IncludeParticipantDevices()
              .SelectDevices()
              .ExcludeUser(excludeUniversalUserId)
              .ToArrayAsync();
            
            return devices;
        }
    }
}
