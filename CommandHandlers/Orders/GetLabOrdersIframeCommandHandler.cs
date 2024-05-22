using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.ChangeHealthCare.Clients.Models.Orders;
using WildHealth.ChangeHealthCare.Clients.Models.Patients;
using WildHealth.ChangeHealthCare.Clients.Options;
using WildHealth.ChangeHealthCare.Clients.WebClient;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Exceptions;
using WildHealth.Settings;
using MediatR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Integrations;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Integration;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Utils.AuthTicket;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class GetLabOrdersIframeCommandHandler : IRequestHandler<GetLabOrdersIframeCommand, string>
    {
        private static readonly string[] SettingNames =
        {
            SettingsNames.ChangeHealthCare.BaseUrl,
            SettingsNames.ChangeHealthCare.UserId,
            SettingsNames.ChangeHealthCare.Password,
            SettingsNames.ChangeHealthCare.HdnBusiness,
            SettingsNames.ChangeHealthCare.DefaultCareGiver
        };
        
        private readonly IPatientsService _patientService;
        private readonly ISettingsManager _settingsManager;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IChangeHealthCareWebClient _webClient;
        private readonly ILogger<GetLabOrdersIframeCommandHandler> _logger;
        private readonly IIntegrationsService _integrations;
        private readonly IAuthTicket _authTicket;
        private readonly CHCLabOrderIframeOptions _labOrderOptions;

        public GetLabOrdersIframeCommandHandler(
            IPatientsService patientService,
            ISettingsManager settingsManager,
            IPermissionsGuard permissionsGuard,
            IChangeHealthCareWebClient webClient, 
            ILogger<GetLabOrdersIframeCommandHandler> logger,
            IIntegrationsService integrations,
            IAuthTicket authTicket,
            IOptions<CHCLabOrderIframeOptions> labOrderOptions)
        {
            _patientService = patientService;
            _settingsManager = settingsManager;
            _permissionsGuard = permissionsGuard;
            _webClient = webClient;
            _logger = logger;
            _integrations = integrations;
            _authTicket = authTicket;
            _labOrderOptions = labOrderOptions.Value;
        }
        
        public async Task<string> Handle(GetLabOrdersIframeCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientService.GetByIdAsync(command.PatientId);
            
            _permissionsGuard.AssertPermissions(patient);
            
            var settings = await _settingsManager.GetSettings(SettingNames, patient.User.PracticeId);

            var options = ConvertToOptions(settings);

            var userId = _authTicket.GetId();
            
            var integration = await _integrations.GetUserIntegrationAsync(userId, 
                                                                          IntegrationVendor.ChangeHealthCare,
                                                                          IntegrationPurposes.Employee.ChangeHealthCareCredential);


            if (_labOrderOptions.RequireUserIntegrationForAuth)
            {
                if (integration == null)
                {
                    var message = $"User {userId} does not have a CHC integration";
                    _logger.LogInformation(message);

                    // If you need one, see:
                    // WildHealth.WebApi/scripts/ChangeHealthCareUserIntegration/createUserIntegration.csx
                    throw new AppException(HttpStatusCode.Unauthorized, message);
                }
                SetChcIntegrationCredential(integration, options);
            }
            else
            {
                if (integration == null)
                {
                    _logger.LogInformation($"The user {userId} does not have a CHC integration. Using the default credentials instead.");
                }
                else
                {
                    SetChcIntegrationCredential(integration, options);
                }
            }

            _webClient.Initialize(options);
            
            var matchPatientModel = new MatchPatientModel
            {
                DayOfBirth = patient.User.Birthday ?? DateTime.MinValue,
                Sex = GetGenderAsString(patient.User.Gender),
                FirstName = patient.User.FirstName,
                LastName = patient.User.LastName,
                PatientId = patient.GetId()
            };

            try
            {
                await _webClient.MatchPatientAsync(matchPatientModel);

                var getOrdersModel = new GetOrdersModel(patient.GetId());

                return _webClient.GetOrdersIFrameLink(getOrdersModel);
            }
            catch (AppException e)
            {
                _logger.LogError($"Error getting iframe from the CHC - {e}");
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error getting iframe from the CHC - {e}");
                
                throw new AppException(HttpStatusCode.BadRequest, "Cannot create lab order. Please try again later or contact support.", e);
            }
        }

        private void SetChcIntegrationCredential(UserIntegration integration, ChangeHealthCareOptions options)
        {
            try
            {
                var creds = JsonConvert.DeserializeObject<ChangeHealthCareUserIntegrationValue>(integration.Integration.Value)!;
                options.UserId = creds.Username;
                options.Password = creds.Password;
            }
            catch (Exception e)
            {
                _logger.LogError($"Cannot retrieve CHC integration for user {integration.UserId}: {e}");
                throw new AppException(HttpStatusCode.NotFound, "CHC Integration is not found", e);
            }
        }

        #region private 
        
        /// <summary>
        /// Converts Clarity gender to Change health care gender
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
                DefaultCareGiver = settings[SettingsNames.ChangeHealthCare.DefaultCareGiver]
            };
        }
        
        #endregion
    }
}
