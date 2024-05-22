using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.AddOns;
using WildHealth.ChangeHealthCare.Clients.Models.Orders;
using WildHealth.ChangeHealthCare.Clients.WebClient;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Shared.Exceptions;
using WildHealth.ChangeHealthCare.Clients.Models.Patients;
using WildHealth.Domain.Enums.User;
using WildHealth.ChangeHealthCare.Clients.Options;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class PlaceLabOrderCommandHandler : IRequestHandler<PlaceLabOrderCommand>
    {
        private static readonly string[] SettingNames =
        {
            SettingsNames.ChangeHealthCare.BaseUrl,
            SettingsNames.ChangeHealthCare.UserId,
            SettingsNames.ChangeHealthCare.Password,
            SettingsNames.ChangeHealthCare.HdnBusiness,
            SettingsNames.ChangeHealthCare.DefaultCareGiver
        };
        
        private readonly IAddOnsService _addOnsService;
        private readonly IChangeHealthCareWebClient _webClient;
        private readonly ISettingsManager _settingsManager;
        private readonly ILogger _logger;

        public PlaceLabOrderCommandHandler(
            IAddOnsService addOnsService, 
            IChangeHealthCareWebClient webClient, 
            ISettingsManager settingsManager,
            ILogger<PlaceLabOrderCommandHandler> logger)
        {
            _addOnsService = addOnsService;
            _webClient = webClient;
            _settingsManager = settingsManager;
            _logger = logger;
        }

        public async Task Handle(PlaceLabOrderCommand command, CancellationToken cancellationToken)
        {
            var patient = command.Patient;
            
            _logger.LogInformation($"Placing Lab order for patient with id: {patient.GetId()} has been started.");

            var settings = await _settingsManager.GetSettings(SettingNames, patient.User.PracticeId);

            var options = ConvertToOptions(settings);
            
            _webClient.Initialize(options);
            
            var addOns = await FetchAddOnsAsync(command.AddOnIds, patient.User.PracticeId);

            AssertAddOnsType(addOns);
            
            var patientModel = CreatePatientModel(patient);

            var personId = await _webClient.CreatePatientAsync(patientModel);

            var orderModel = CreateModel(patient, personId, addOns);

            await _webClient.PlaceOrderAsync(orderModel);

            _logger.LogInformation($"Placing Lab order for patient with id: {patient.GetId()} has been finished.");
        }
        
        #region private

        /// <summary>
        /// Asserts if add-on types matches with order type
        /// </summary>
        /// <param name="addOns"></param>
        /// <exception cref="AppException"></exception>
        private void AssertAddOnsType(AddOn[] addOns)
        {
            if (addOns.Any(x => x.OrderType != OrderType.Lab))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Add on type and order type does not match.");
            }
        }
        
        /// <summary>
        /// Fetches and returns add-ons by ids
        /// </summary>
        /// <param name="addOnIds"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        private async Task<AddOn[]> FetchAddOnsAsync(int[] addOnIds, int practiceId)
        {
            var addOns = await _addOnsService.GetByIdsAsync(addOnIds, practiceId);

            return addOns.ToArray();
        }

        private CreatePatientModel CreatePatientModel(Patient patient)
        {
            return new CreatePatientModel
            {
                PatientId = patient.GetId().ToString(),
                FirstName = patient.User.FirstName,
                LastName = patient.User.LastName,
                DayOfBirth = patient.User.Birthday ?? DateTime.UtcNow,
                Sex = GetGenderAsString(patient.User.Gender)
            };
        }
        
        /// <summary>
        /// Creates and returns order items based on add-ons
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="personId"></param>
        /// <param name="addOns"></param>
        /// <returns></returns>
        private PlaceOrderModel CreateModel(
            Patient patient,  
            string personId,
            AddOn[] addOns)
        {
            var laboratory = addOns.First().Provider.ToString();
                
            var items = addOns.Select(x => new PlaceOrderItemModel(
                testCode: x.IntegrationId,
                testName: x.Name
            )).ToArray();
            
            return new PlaceOrderModel(
                items: items,
                laboratory: laboratory,
                personId: personId,
                patientId: patient.GetId().ToString(),
                patientFirstName: patient.User.FirstName,
                patientLastName: patient.User.LastName,
                patientSex: GetGenderAsString(patient.User.Gender),
                dayOfBirth: patient.User.Birthday ?? DateTime.UtcNow
            );
        }
        
        /// <summary>
        /// Converts System gender to Change health care gender
        /// </summary>
        /// <param name="gender"></param>
        /// <returns></returns>
        private string GetGenderAsString(Gender gender)
        {
            return gender switch
            {
                Gender.Male => "M",
                Gender.Female => "F",
                Gender.None => "",
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
            };
        }

        /// <summary>
        /// Converts settings to options
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private ChangeHealthCareOptions ConvertToOptions(IDictionary<string, string> settings)
        {
            return new ChangeHealthCareOptions
            {
                BaseUrl = settings[SettingsNames.ChangeHealthCare.BaseUrl],
                UserId = settings[SettingsNames.ChangeHealthCare.UserId],
                Password = settings[SettingsNames.ChangeHealthCare.Password],
                HdnBusiness = settings[SettingsNames.ChangeHealthCare.HdnBusiness],
                DefaultCareGiver = settings[SettingsNames.ChangeHealthCare.DefaultCareGiver],
            };
        }

        #endregion
    }
}