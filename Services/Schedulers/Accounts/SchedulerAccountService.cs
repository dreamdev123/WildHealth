using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Application.Services.Schedulers.Base;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;
using WildHealth.TimeKit.Clients.Constants;
using WildHealth.TimeKit.Clients.Models.Availability;
using WildHealth.TimeKit.Clients.Models.Resources;
using WildHealth.TimeKit.Clients.WebClient;

namespace WildHealth.Application.Services.Schedulers.Accounts
{
    public class SchedulerAccountService: SchedulerBaseService, ISchedulerAccountService
    {
        private readonly ITimeKitWebClient _client;

        public SchedulerAccountService(
            ITimeKitWebClient client,
            ISettingsManager settingsManager) : base(settingsManager)
        {
            _client = client;
        }

        /// <summary>
        /// <see cref="ISchedulerAccountService.RegisterAccountAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<SchedulerRegistrationResultModel> RegisterAccountAsync(RegisterSchedulerAccountModel model)
        {
            _client.Initialize(await GetBookingCredentialsAsync(model.PracticeId));

            var password = model.Password ?? Guid.NewGuid().ToString("d").Substring(0, 7);

            var requestModel = new CreateResourceModel
            {
                Timezone = TimeKitConstants.TimeZones.Default,
                Email = model.Email,
                Name = $"{model.FirstName} {model.LastName}",
                FirstName = model.FirstName,
                LastName = model.LastName,
                Password = password,
                AvailabilityConstraints = GetDefaultAvailability()
            };

            var result = await _client.CreateResourceAsync(requestModel);

            return new SchedulerRegistrationResultModel
            {
                Id = result.Id,
                Password = password
            };
        }

        /// <summary>
        /// <see cref="ISchedulerAccountService.DeleteAccountAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAccountAsync(DeleteSchedulerAccountModel model)
        {
            _client.Initialize(await GetBookingCredentialsAsync(model.PracticeId));

            await _client.DeleteResourceAsync(model.AccountId);

            return true;
        }
        
        /// <summary>
        /// <see cref="ISchedulerAccountService.GetAccountAsync"/>
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        public async Task<ResourceModel> GetAccountAsync(Employee employee)
        {
            if (string.IsNullOrEmpty(employee.SchedulerAccountId))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Appointments for your account is not configured. Please contact support.");
            }
            
            _client.Initialize(await GetBookingCredentialsAsync(employee.User.PracticeId));

            return await _client.GetResourceAsync(employee.SchedulerAccountId);
        }

        /// <summary>
        /// <see cref="ISchedulerAccountService.GetResourcesByEmailAsync(Employee)"/>
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<ResourceModel[]> GetResourcesByEmailAsync(Employee employee)
        {
            if (employee?.User is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "User is not included.");
            }
            
            _client.Initialize(await GetBookingCredentialsAsync(employee.User.PracticeId));

            return await _client.GetResourcesByEmailAsync(employee.User.Email);
        }

        /// <summary>
        /// <see cref="ISchedulerAccountService.GetResourcesAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<ResourceAccountModel[]> GetResourcesAsync(int practiceId)
        {
            _client.Initialize(await GetBookingCredentialsAsync(practiceId));

            var result = new List<ResourceAccountModel>();
            var page = 1;
            
            while (true)
            {
                var resources = await _client.GetResourcesAccountsAsync(page++);
                
                result.AddRange(resources);

                if (resources.Length < 50)
                {
                    return result.ToArray();
                }
            }
        }

        #region private

        private AvailabilityConstraintModel[] GetDefaultAvailability()
        {
            return new []
            {
                new AvailabilityConstraintModel
                {
                    AllowedDayTime = new AllowedDayTimeModel
                    {
                        Day = "Monday",
                        Start = 7,
                        End = 21
                    }
                },
                new AvailabilityConstraintModel
                {
                    AllowedDayTime = new AllowedDayTimeModel
                    {
                        Day = "Tuesday",
                        Start = 7,
                        End = 21
                    }
                },
                new AvailabilityConstraintModel
                {
                    AllowedDayTime = new AllowedDayTimeModel
                    {
                        Day = "Wednesday",
                        Start = 7,
                        End = 21
                    }
                },
                new AvailabilityConstraintModel
                {
                    AllowedDayTime = new AllowedDayTimeModel
                    {
                        Day = "Thursday",
                        Start = 7,
                        End = 21
                    }
                },
                new AvailabilityConstraintModel
                {
                    AllowedDayTime = new AllowedDayTimeModel
                    {
                        Day = "Friday",
                        Start = 7,
                        End = 21
                    }
                }
            };
        }

        #endregion
    }
}
