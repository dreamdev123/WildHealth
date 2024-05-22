using System.Threading.Tasks;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Zoom.Clients.Constants;
using WildHealth.Zoom.Clients.Models.Meetings;

namespace WildHealth.Application.Services.Schedulers.Meetings
{
    public interface ISchedulerMeetingsService
    {
        /// <summary>
        /// Creates meeting in meeting system.
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="appointment"></param>
        /// <param name="autoRecording"></param>
        /// <param name="ownerEmail"></param>
        /// <returns>Scheduler meeting model</returns>
        Task<MeetingModel> CreateMeetingAsync(
            int practiceId, 
            Appointment appointment,
            string? autoRecording = ZoomConstants.AutoRecording.None,
            string? ownerEmail = null);

        /// <summary>
        /// Deletes meeting in meeting system
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="meetingId"></param>
        /// <param name="ownerEmail"></param>
        /// <returns></returns>
        Task DeleteMeetingAsync(int practiceId, long meetingId, string ownerEmail);
    }
}