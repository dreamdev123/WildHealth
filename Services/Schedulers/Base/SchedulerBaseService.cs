using System.Threading.Tasks;
using Hangfire.Dashboard.Resources;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using BookingCredentials = WildHealth.TimeKit.Clients.Credentials.CredentialsModel;
using MeetingCredentials = WildHealth.Zoom.Clients.Credentials.CredentialsModel;

namespace WildHealth.Application.Services.Schedulers.Base
{
    public abstract class SchedulerBaseService
    {
        private readonly string[] _bookingSettingKeys = { SettingsNames.TimeKit.Url, SettingsNames.TimeKit.ApiKey };
        private readonly string[] _meetingSettingKeys = { SettingsNames.Zoom.Url, SettingsNames.Zoom.UserId, 
            SettingsNames.Zoom.ApiKey, SettingsNames.Zoom.ApiSecret, SettingsNames.Zoom.OAuthUrl, 
            SettingsNames.Zoom.OAuthAccountId, SettingsNames.Zoom.OAuthClientId,SettingsNames.Zoom.OAuthClientSecret  };

        private readonly ISettingsManager _settingsManager;

        protected SchedulerBaseService(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        protected async Task<BookingCredentials> GetBookingCredentialsAsync(int practiceId)
        {
            var settings = await _settingsManager.GetSettings(_bookingSettingKeys, practiceId);

            return new BookingCredentials(
                url: settings[SettingsNames.TimeKit.Url],
                apiKey: settings[SettingsNames.TimeKit.ApiKey]
            );
        }

        protected async Task<MeetingCredentials> GetMeetingCredentialsAsync(int practiceId, string? ownerEmail)
        {
            var settings = await _settingsManager.GetSettings(_meetingSettingKeys, practiceId);

            return new MeetingCredentials(
                url: settings[SettingsNames.Zoom.Url],
                userId: string.IsNullOrEmpty(ownerEmail) ? settings[SettingsNames.Zoom.UserId] : ownerEmail,
                apiKey: settings[SettingsNames.Zoom.ApiKey],
                apiSecret: settings[SettingsNames.Zoom.ApiSecret],
                oAuthUrl: settings[SettingsNames.Zoom.OAuthUrl],
                oAuthAccountId: settings[SettingsNames.Zoom.OAuthAccountId],
                oAuthClientId: settings[SettingsNames.Zoom.OAuthClientId],
                oAuthClientSecret: settings[SettingsNames.Zoom.OAuthClientSecret]
            );
        }
    }
}
