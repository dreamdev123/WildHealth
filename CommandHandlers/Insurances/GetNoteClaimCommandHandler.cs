using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Notes;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Fhir.Models.Claims;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class GetNoteClaimCommandHandler : IRequestHandler<GetNoteClaimCommand, ClaimModel?>
{
    private readonly INoteService _noteService;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;

    public GetNoteClaimCommandHandler(
        INoteService noteService,
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory)
    {
        _noteService = noteService;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
    }

    public async Task<ClaimModel?> Handle(GetNoteClaimCommand command, CancellationToken cancellationToken)
    {
        var note = await _noteService.GetByIdAsync(command.NoteId);

        var appointment = note.Appointment;

        if (appointment is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Note with id = {command.NoteId} is not associated with an appointment.");
        }

        var user = note.Patient.User;

        var appointmentDomain = AppointmentDomain.Create(appointment);
        
        var appointmentIntegration = appointmentDomain.GetIntegration(IntegrationPurposes.Appointment.ExternalId);

        if (appointmentIntegration is null)
        {
            return null;
        }
        
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(
            vendor: appointmentIntegration.Vendor,
            practiceId: user.PracticeId);

        var pmAppointmentId = appointmentIntegration.Value;
        var pmPatientId = user.GetIntegrationId(pmService.Vendor, IntegrationPurposes.User.Customer);
        
        if (string.IsNullOrEmpty(pmPatientId) || string.IsNullOrEmpty(pmAppointmentId))
        {
            return null;
        }

        var claims = await pmService.QueryClaimsAsync(
            practiceId: user.PracticeId,
            pmPatientId: pmPatientId);

        var claim = claims.FirstOrDefault(o => o.Extensions.Any(k =>
        {
            var extension = k.Extensions.FirstOrDefault();
            return extension?.Id == "linked_appointment" &&
                   extension.Value.Codings.FirstOrDefault()?.Code == pmAppointmentId;
        }));

        return claim;
    }
}