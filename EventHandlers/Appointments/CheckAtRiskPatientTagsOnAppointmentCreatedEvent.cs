using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Services.Patients;

namespace WildHealth.Application.EventHandlers.Appointments;

public class CheckAtRiskPatientTagsOnAppointmentCreatedEvent : INotificationHandler<AppointmentCreatedEvent>
{
    private readonly IMediator _mediator;
    private readonly IPatientsService _patientsService;

    public CheckAtRiskPatientTagsOnAppointmentCreatedEvent(
        IPatientsService patientsService, 
        IMediator mediator)
    {
        _patientsService = patientsService;
        _mediator = mediator;
    }

    public async Task Handle(AppointmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.PatientId.HasValue) return;
        
        var patient = await _patientsService.GetByIdAsync(notification.PatientId.Value);
        
        await _mediator.Send(new CheckAtRiskPatientTagsCommand(patient), cancellationToken);
    }
}