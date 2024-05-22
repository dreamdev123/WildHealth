// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using WildHealth.Application.Events.Patients.Insurance;
// using WildHealth.OpenPM.Clients.WebClients;
// using WildHealth.Settings;
// using WildHealth.Common.Constants;
// using WildHealth.OpenPM.Clients.Models.Patients;
// using MediatR;

// namespace WildHealth.Application.EventHandlers.Patients
// {
//     public class CreateOpenPMPatientOnPersonalInformationEnteredEvent : INotificationHandler<PersonalInformationEnteredEvent>
//     {
//         private static readonly string[] OpenPmCredentialsSettings =
//         {
//             SettingsNames.OpenPM.Url,
//             SettingsNames.OpenPM.GrantType,
//             SettingsNames.OpenPM.ClientId,
//             SettingsNames.OpenPM.ClientSecret,
//             SettingsNames.OpenPM.AccessTokenRefreshInSeconds
//         };

//         private readonly IMediator _mediator;
//         private readonly IOpenPMWebClient _openPmWebClient;
//         private readonly ISettingsManager _settingsManager;

//         public CreateOpenPMPatientOnPersonalInformationEnteredEvent(IMediator mediator, IOpenPMWebClient openPmWebClient, ISettingsManager settingsManager)
//         {
//             _mediator = mediator;
//             _openPmWebClient = openPmWebClient;
//             _settingsManager = settingsManager;
//         }

//         public async Unit Handle(PersonalInformationEnteredEvent notification, CancellationToken cancellationToken)
//         {

//             var settings = await _settingsManager.GetSettings(OpenPmCredentialsSettings, notification.ForPracticeId);

//             var credentials = new WildHealth.OpenPM.Clients.Credentials.CredentialsModel(
//                 settings[SettingsNames.OpenPM.Url],
//                 settings[SettingsNames.OpenPM.ClientId],
//                 settings[SettingsNames.OpenPM.ClientSecret],
//                 settings[SettingsNames.OpenPM.GrantType],
//                 Convert.ToInt16(settings[SettingsNames.OpenPM.AccessTokenRefreshInSeconds])
//             );

//             // check if user with email exists, if yes then use that user
//             // -- If user exists, then see if user has any existing integrations with OpenPM
//             // -- -- If yes, then use it
//             // -- -- If no, then we need to create a patient here and link it 

//             // _openPmWebClient.CreatePatient()
//         }
//     }
// }