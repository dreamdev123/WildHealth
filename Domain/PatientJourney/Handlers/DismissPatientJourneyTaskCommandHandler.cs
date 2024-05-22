using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Domain.PatientJourney.Commands;
using WildHealth.Application.Domain.PatientJourney.Flows;
using WildHealth.Application.Domain.PatientJourney.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.Domain.PatientJourney.Handlers;

public class DismissPatientJourneyTaskCommandHandler : IRequestHandler<DismissPatientJourneyTaskCommand>
{
    private readonly IPatientJourneyService _patientJourneyService;
    private readonly MaterializeFlow _materializer;

    public DismissPatientJourneyTaskCommandHandler(IPatientJourneyService patientJourneyService, MaterializeFlow materializer)
    {
        _patientJourneyService = patientJourneyService;
        _materializer = materializer;
    }

    public async Task Handle(DismissPatientJourneyTaskCommand request, CancellationToken cancellationToken)
    {
        var journeyTask = await _patientJourneyService.GetJourneyTask(request.JourneyTaskId);
        var patientJourneyTask = await _patientJourneyService.GetPatientJourneyTask(request.PatientId, request.JourneyTaskId).ToOption();

        await new DismissPatientJourneyTaskFlow(request.PatientId, journeyTask, patientJourneyTask).Materialize(_materializer);
    }
}