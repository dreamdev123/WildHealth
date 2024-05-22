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

public class UndoPatientJourneyTaskCommandHandler : IRequestHandler<UndoPatientJourneyTaskCommand>
{
    private readonly IPatientJourneyTreeBuilder _journeyTreeBuilder;
    private readonly IPatientJourneyService _patientJourneyService;
    private readonly MaterializeFlow _materializer;

    public UndoPatientJourneyTaskCommandHandler(
        IPatientJourneyTreeBuilder journeyTreeBuilder, 
        IPatientJourneyService patientJourneyService, 
        MaterializeFlow materializer)
    {
        _journeyTreeBuilder = journeyTreeBuilder;
        _patientJourneyService = patientJourneyService;
        _materializer = materializer;
    }

    public async Task Handle(UndoPatientJourneyTaskCommand request, CancellationToken cancellationToken)
    {
        var patientJourneyTask = await _patientJourneyService.GetPatientJourneyTask(request.PatientId, request.JourneyTaskId).ToOption();
        var journeyTree = await _journeyTreeBuilder.Build(request.PatientId, request.PaymentPlanId, request.PaymentPlanId);
        
        await new UndoPatientJourneyTaskFlow(patientJourneyTask, journeyTree).Materialize(_materializer);
    }
}