using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Notes;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Fhir.Models.PaymentRecs;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class GetNoteRemitCommandHandler : IRequestHandler<GetNoteRemitCommand, PaymentRecModel[]?>
{
    private readonly INoteService _noteService;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly IMediator _mediator;

    public GetNoteRemitCommandHandler(
        INoteService noteService,
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        IMediator mediator)
    {
        _noteService = noteService;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _mediator = mediator;
    }

    public async Task<PaymentRecModel[]?> Handle(GetNoteRemitCommand command, CancellationToken cancellationToken)
    {
        var note = await _noteService.GetByIdAsync(command.NoteId);

        if (note.Appointment is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Note with id = {command.NoteId} is not associated with an appointment.");
        }

        var appointmentDomain = AppointmentDomain.Create(note.Appointment);
        
        var appointmentIntegration = appointmentDomain.GetIntegration(IntegrationPurposes.Appointment.ExternalId);

        if (appointmentIntegration is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Appointment for note id = {command.NoteId} is not associated with a PM service");
        }
        
        var claim = await _mediator.Send(new GetNoteClaimCommand(note.GetId()));
        
        if (claim is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Claim not found for note id = {command.NoteId}");
        }
        
        var user = note.Patient.User;

        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(
            vendor: appointmentIntegration.Vendor,
            practiceId: user.PracticeId);

        var paymentRecs = await pmService.QueryPaymentRecsAsync(user.PracticeId, claim.Id);

        return paymentRecs;
    }
}