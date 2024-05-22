using MediatR;

namespace WildHealth.Application.Events.Schedulers
{
    public class SchedulerAccountCreatedEvent : INotification
    {
        public int PracticeId { get; }

        public string FirstName { get; }

        public string Email { get; }

        public string SchedulerPassword { get; }

        public SchedulerAccountCreatedEvent(int practiceId,
            string firstName,
            string email,
            string schedulerPassword)
        {
            PracticeId = practiceId;
            FirstName = firstName;
            Email = email;
            SchedulerPassword = schedulerPassword;
        }
    }
}
