using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Payments;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Application.Commands.Patients;
using MediatR;

namespace WildHealth.Application.EventHandlers.Payments
{
    public class SendPatientToIntakeOnSubscriptionChangedEvent : INotificationHandler<SubscriptionChangedEvent>
    {
        private readonly IPatientsService _patientsService;
        private readonly IMediator _mediator;

        public SendPatientToIntakeOnSubscriptionChangedEvent(
            IPatientsService patientsService,
            IMediator mediator)
        {
            _patientsService = patientsService;
            _mediator = mediator;
        }

        public async Task Handle(SubscriptionChangedEvent notification, CancellationToken cancellationToken)
        {
            var patient = notification.Patient;
            if (patient.GetAssignedEmployees().Any())
            {
                return;
            }

            if (patient.OnBoardingStatus == PatientOnBoardingStatus.New)
            {
                return;
            }

            await _patientsService.UpdatePatientOnBoardingStatusAsync(patient, PatientOnBoardingStatus.New);

            var enrollmentCommand = new SendNewEnrollmentNotificationCommand(
                practiceId: patient.User.PracticeId,
                locationId: patient.LocationId,
                patientId: patient.GetId(),
                notification.NewSubscription.GetId());
            
            await _mediator.Send(enrollmentCommand, cancellationToken);
        }
    }
}
