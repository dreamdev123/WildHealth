using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Application.Services.Schedulers.Base;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;
using WildHealth.TimeKit.Clients.Exceptions;
using WildHealth.TimeKit.Clients.Models.Bookings;
using WildHealth.TimeKit.Clients.Models.Customers;
using WildHealth.TimeKit.Clients.WebClient;

namespace WildHealth.Application.Services.Schedulers.Bookings
{
    public class SchedulerBookingsService : SchedulerBaseService, ISchedulerBookingsService
    {
        private readonly ITimeKitWebClient _client;
        
        public SchedulerBookingsService(
            ITimeKitWebClient client,
            ISettingsManager settingsManager) : base(settingsManager)
        {
            _client = client;
        }

        /// <summary>
        /// <see cref="ISchedulerBookingsService.CreateBookingAsync(int,Appointment,System.Collections.Generic.IEnumerable{WildHealth.Domain.Entities.Employees.Employee},Patient)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="appointment"></param>
        /// <param name="employees"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<BookingModel> CreateBookingAsync(int practiceId,
            Appointment appointment,
            IEnumerable<Employee> employees,
            Patient? patient)
        {
            if (!employees.Any())
            {
                throw new AppException(HttpStatusCode.BadRequest, "Appointment does not have assigment employees");
            }
            
            _client.Initialize(await GetBookingCredentialsAsync(practiceId));

            var customer = patient is null
                ? GetNewCustomerModel(employees.Last().User)
                : GetNewCustomerModel(patient.User);

            var meetingOwner = employees.First();
            var participants = employees
                .Select(e => e.User.Email)
                .Concat(new [] { customer.Email })
                .ToArray();

            var createBookingModel = new CreateBookingModel
            {
                Start = ToTimeKitDateTime(appointment.StartDate),
                End = ToTimeKitDateTime(appointment.EndDate),
                UserId = meetingOwner.SchedulerAccountId,
                What = appointment.Name,
                Where = appointment.LocationType.ToString(),
                Description = GetDescription(appointment),
                Graph = "instant",
                Customer = customer,
                Participants = participants
            };

            return await _client.CreateBookingAsync(createBookingModel);
        }

        /// <summary>
        /// <see cref="ISchedulerBookingsService.CancelBookingAsync(int,Appointment)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="appointment"></param>
        /// <returns></returns>
        public async Task CancelBookingAsync(int practiceId, Appointment appointment)
        {
            if (string.IsNullOrEmpty(appointment.SchedulerSystemId))
            {
                return;
            }
            
            _client.Initialize(await GetBookingCredentialsAsync(practiceId));

            var cancelModel = new CancelModel
            {
                Message = "Cancelled"
            };
            
            try
            {
                await _client.CancelBookingAsync(appointment.SchedulerSystemId, cancelModel);
            }
            catch (TimeKitException e) when(e.Message == "Cannot apply action 'cancel' to current state 'cancelled'")
            {
                // Ignore exception when appointment already cancelled in origin source
            }
        }

        /// <summary>
        /// Gets from: https://developers.timekit.io/reference#timestamps
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private string ToTimeKitDateTime(DateTime dateTime)
        {
            var localTimeWithOffset = new DateTimeOffset(dateTime, TimeZoneInfo.Utc.BaseUtcOffset);
            var isoDate = localTimeWithOffset.ToString("yyyy-MM-ddTHH:mm:ssK");
            
            return isoDate;
        }

        private string GetDescription(Appointment appointment)
        {
            var joinLink = $"Join link: {appointment.JoinLink}";

            return string.IsNullOrEmpty(appointment.Comment) 
                ? joinLink 
                : $"{appointment.Comment}\n\n{joinLink}";
        }

        private string GetUserName(User user)
        {
            return $"{user.FirstName} {user.LastName}";
        }

        private CustomerModel GetNewCustomerModel(User user)
        {
            return new CustomerModel
            {
                Email = user.Email,
                Name = GetUserName(user)
            };
        }
    }
}