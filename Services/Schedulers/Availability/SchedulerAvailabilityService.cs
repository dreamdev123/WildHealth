using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Application.Services.Schedulers.Base;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Shared.Exceptions;
using WildHealth.TimeKit.Clients.Constants;
using WildHealth.TimeKit.Clients.Extensions;
using WildHealth.TimeKit.Clients.Models.Availability;
using WildHealth.TimeKit.Clients.WebClient;
using WildHealth.Application.Services.Employees;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Application.Services.Appointments;
using WildHealth.Settings;
using AutoMapper;
using WildHealth.Application.Utils.Schedulers;
using WildHealth.Domain.Entities.Appointments;

namespace WildHealth.Application.Services.Schedulers.Availability
{
    public class SchedulerAvailabilityService: SchedulerBaseService, ISchedulerAvailabilityService
    {
        private readonly IAppointmentsService _appointmentsService;
        private readonly IEmployeeService _employeeService;
        private readonly ITimeKitWebClient _client;
        private readonly IMapper _mapper;
        private readonly IAvailabilityHelper _availabilityHelper;

        public SchedulerAvailabilityService(
            IAppointmentsService appointmentsService,
            ISettingsManager settingsManager,
            IEmployeeService employeeService,
            ITimeKitWebClient client,
            IMapper mapper,
            IAvailabilityHelper availabilityHelper
            ) : base(settingsManager)
        {
            _appointmentsService = appointmentsService;
            _employeeService = employeeService;            
            _client = client;
            _mapper = mapper;
            _availabilityHelper = availabilityHelper;
        }

        /// <summary>
        /// <see cref="ISchedulerAvailabilityService.GetAvailabilityAsync(int, int?, int[], DateTime, DateTime)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="configurationId"></param>
        /// <param name="employeeIds"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<SchedulerAvailabilityModel[]> GetAvailabilityAsync(
            int practiceId, 
            int? configurationId,
            int[] employeeIds, 
            DateTime from, 
            DateTime to)
        {
            var employees = await _employeeService.GetByIdsAsync(employeeIds, EmployeeSpecifications.Empty);
            
            return await GetAvailabilityAsync(
                practiceId: practiceId,
                configurationId: configurationId,
                employees: employees,
                from: from,
                to: to
            );
        }

        /// <summary>
        /// <see cref="ISchedulerAvailabilityService.GetAvailabilityAsync(int, int?, Employee[], DateTime, DateTime)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="configurationId"></param>
        /// <param name="employees"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<SchedulerAvailabilityModel[]> GetAvailabilityAsync(
            int practiceId, 
            int? configurationId,
            Employee[] employees, 
            DateTime from, 
            DateTime to)
        {
            if (employees.Any(x=> string.IsNullOrEmpty(x.SchedulerAccountId)))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Appointments for your account is not configured. Please contact support.");
            }
            
            var configuration = await GetConfigurationAsync(practiceId, configurationId);

            _client.Initialize(await GetBookingCredentialsAsync(practiceId));

            var duration = configuration?.Duration ?? AvailabilityConstants.DefaultAvailabilityDuration;

            var increment = configuration is not null
                ? RoundOff(configuration.Duration)
                : AvailabilityConstants.DefaultAvailabilityIncrement;

            var requestModel = GetAvailabilityQueryModel(
                employeeSchedulerIds: employees.Select(x => x.SchedulerAccountId).ToArray(),
                from: from,
                to: to,
                duration: duration,
                increment: AvailabilityConstants.DefaultAvailabilityIncrement
            );

            var result = await _client.GetAvailabilityAsync(requestModel);

            var formattedSet = _availabilityHelper.FormatResults(result, increment);
            
            return _mapper.Map<SchedulerAvailabilityModel[]>(formattedSet.Select(x=> x.ToUtc()));
        }

        /// <summary>
        /// <see cref="ISchedulerAvailabilityService.GetCommonUsersAvailabilityAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="employeeSchedulerIds"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<SchedulerAvailabilityModel[]> GetCommonUsersAvailabilityAsync(
            int practiceId, 
            string[] employeeSchedulerIds, 
            DateTime from, 
            DateTime to)
        {
            _client.Initialize(await GetBookingCredentialsAsync(practiceId));

            if (employeeSchedulerIds.Length == 0)
            {
                throw new AppException(HttpStatusCode.NotFound, $"No employees to find");
            }

            if (employeeSchedulerIds.Any(string.IsNullOrEmpty))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Appointments for your account is not configured. Please contact support.");
            }

            var requestModel = GetAvailabilityQueryModel(
                employeeSchedulerIds: employeeSchedulerIds,
                from: from,
                to: to,
                increment: AvailabilityConstants.DefaultAvailabilityIncrement,
                duration: AvailabilityConstants.DefaultAvailabilityDuration
            );

            var result = await _client.GetAvailabilityAsync(requestModel);

            var formattedSet = _availabilityHelper.FormatResults(result, AvailabilityConstants.DefaultAvailabilityIncrement);
            
            return _mapper.Map<SchedulerAvailabilityModel[]>(formattedSet.Select(x=> x.ToUtc()));
        }
        
        /// <summary>
        /// <see cref="ISchedulerAvailabilityService.GetAvailabilityCountAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="schedulerAccountId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<int> GetAvailabilityCountAsync(
            int practiceId, 
            string schedulerAccountId, 
            DateTime from, 
            DateTime to,
            int duration)
        {
            if (string.IsNullOrEmpty(schedulerAccountId))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Appointments for your account is not configured. Please contact support.");
            }
            
            _client.Initialize(await GetBookingCredentialsAsync(practiceId));

            var requestModel = GetAvailabilityQueryModel(
                employeeSchedulerIds: new[] {schedulerAccountId},
                from: from,
                to: to,
                duration: duration,
                increment: TimeKitConstants.TimeSpansInMinutes.Minutes30);

            // CLAR-6638
            // Endpoint GetAvailabilityCountAsync doesn't allow query in the past.
            // Use endpoint GetAvailabilityAsync instead and do all calculations on client side
            var result = await _client.GetAvailabilityAsync(requestModel);

            return result.Length;
        }
        
        #region private

        private AvailabilityQueryModel GetAvailabilityQueryModel(
            string[] employeeSchedulerIds, 
            DateTime from, 
            DateTime to,
            int? duration = null,
            int? increment = null)
        {
            return new AvailabilityQueryModel
            {
                Mode = employeeSchedulerIds.Length == 1 ? TimeKitConstants.AvailabilityQueryMode.RoundrobinPrioritized : TimeKitConstants.AvailabilityQueryMode.Mutual,
                Resources = employeeSchedulerIds,
                Length =  duration is > 0 
                    ? $"{duration} minutes"
                    : $"{TimeKitConstants.TimeSpansInMinutes.Minutes5} minutes",
                From = from.ToUniversalTime(),
                To = to.ToUniversalTime(),
                TimeslotIncrements = increment.HasValue ? $"{increment} minutes" :  $"{AvailabilityConstants.DefaultAvailabilityIncrement} minutes",
                OutputTimezone = TimeKitConstants.TimeZones.UtcZero
            };
        }

        private async Task<AppointmentTypeConfiguration?> GetConfigurationAsync(int practiceId, int? configurationId)
        {
            if (configurationId is null)
            {
                return null;
            }

            try
            {
                var (_, configuration) = await _appointmentsService.GetTypeByConfigurationIdAsync(practiceId, configurationId.Value);

                return configuration;

            }
            catch (AppException e) when(e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
        
        private int RoundOff (int i)
        {
            return (int)Math.Ceiling(i / 30.0) * 30;
        }

        #endregion
    }
    
}
