using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Appointments;

public class GetAppointmentsSequenceInfoCommandHandler : IRequestHandler<GetAppointmentsSequenceInfoCommand, AppointmentsSequenceInfoModel>
{
    private readonly IAppointmentsService _appointmentsService;

    public GetAppointmentsSequenceInfoCommandHandler(IAppointmentsService appointmentsService)
    {
        _appointmentsService = appointmentsService;
    }

    public async Task<AppointmentsSequenceInfoModel> Handle(GetAppointmentsSequenceInfoCommand command, CancellationToken cancellationToken)
    {
        var appointments = (await _appointmentsService.GetPatientAppointmentsAsync(command.PatientId)).ToArray();

        var appointment = appointments.FirstOrDefault(x => x.Id == command.Id);

        if (appointment is null)
        {
            throw new DomainException("Appointment does not exist");
        }

        return AppointmentDomain.Create(appointment).SequenceInfo();
    }
}