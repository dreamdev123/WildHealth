using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Utils.AppointmentTag;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Appointments;

public class GetAppointmentsTagsCommandHandler : IRequestHandler<GetAppointmentsTagsCommand, AppointmentTagsModel[]>
{
    private readonly IGeneralRepository<Appointment> _appointmentsRepository;
    private readonly IAppointmentTagsMapperHelper _appointmentTagsMapperHelper;

    public GetAppointmentsTagsCommandHandler(
        IGeneralRepository<Appointment> appointmentsRepository, 
        IAppointmentTagsMapperHelper appointmentTagsMapperHelper)
    {
        _appointmentsRepository = appointmentsRepository;
        _appointmentTagsMapperHelper = appointmentTagsMapperHelper;
    }

    public async Task<AppointmentTagsModel[]> Handle(GetAppointmentsTagsCommand command, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentsRepository
            .All()
            .ById(command.Id)
            .IncludePatient()
            .IncludePatientWithLabs()
            .IncludePatientSubscription()
            .IncludeEmployee()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (appointment is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Appointment does not exist");
        }

        return _appointmentTagsMapperHelper.MapAppointmentTags(appointment);
    }
}