using MediatR;

namespace WildHealth.Application.Events.Patients
{
    public record PatientTransferredToLocationEvent(int PatientId,
        int NewLocationId,
        int OldLocationId) : INotification;
}