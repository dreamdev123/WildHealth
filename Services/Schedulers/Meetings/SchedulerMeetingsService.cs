using System;
using System.Threading.Tasks;
using WildHealth.Application.Services.Schedulers.Base;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Settings;
using WildHealth.Zoom.Clients.Constants;
using WildHealth.Zoom.Clients.Models.Meetings;
using WildHealth.Zoom.Clients.WebClient;

namespace WildHealth.Application.Services.Schedulers.Meetings
{
    public class SchedulerMeetingsService : SchedulerBaseService, ISchedulerMeetingsService
    {
        private readonly IZoomMeetingsWebClient _client;

        public SchedulerMeetingsService(
            IZoomMeetingsWebClient client,
            ISettingsManager settingsManager) : base(settingsManager)
        {
            _client = client;
        }

        /// <summary>
        /// <see cref="ISchedulerMeetingsService.CreateMeetingAsync(int,Appointment,string)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="appointment"></param>
        /// <param name="autoRecording"></param>
        /// <param name="ownerEmail"></param>
        /// <returns></returns>
        public async Task<MeetingModel> CreateMeetingAsync(
        int practiceId, 
        Appointment appointment, 
        string? autoRecording = ZoomConstants.AutoRecording.None,
        string? ownerEmail = null)
        {
            _client.Initialize(await GetMeetingCredentialsAsync(practiceId, ownerEmail));

            var createMeetingModel = new CreateMeetingModel
            {
                Topic = PrepareTopicName(appointment),
                Agenda = appointment.Comment,
                Duration = appointment.Duration,
                StartTime = ToDefaultTimeZone(appointment.StartDate),
                Timezone = ZoomConstants.Default.ZoomTimeZone,
                Type = MeetingType.ScheduledMeeting,
                PracticeId = practiceId,
                Settings = new MeetingSettingsModel()
                {
                    AutoRecording = autoRecording
                }
            };

            return await _client.CreateMeetingAsync(createMeetingModel);
        }

        /// <summary>
        /// <see cref="ISchedulerMeetingsService.DeleteMeetingAsync(int,long,string)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="meetingId"></param>
        /// <param name="ownerEmail"></param>
        public async Task DeleteMeetingAsync(int practiceId, long meetingId, string ownerEmail)
        {
            _client.Initialize(await GetMeetingCredentialsAsync(practiceId, ownerEmail));

            await _client.DeleteMeetingAsync(practiceId,meetingId);
        }

        #region private

        /// <summary>
        /// Returns converted time to default timezone id for Zoom.
        /// </summary>
        /// <param name="startDate"></param>
        /// <returns></returns>
        private DateTime ToDefaultTimeZone(DateTime startDate)
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(ZoomConstants.Default.DefaultTimeZoneId);
            
            return TimeZoneInfo.ConvertTimeFromUtc(startDate, timezone);
        }

        /// <summary>
        /// Prepare and returns topic name based on appointment
        /// </summary>
        /// <param name="appointment"></param>
        /// <returns></returns>
        private string PrepareTopicName(Appointment appointment)
        {
            var topic = appointment.Name;

            if (topic.Length > 126)
            {
                topic = $"{topic[..123]}...";
            }

            return topic;
        }

        #endregion
    }
}