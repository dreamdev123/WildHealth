using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.CallToActions;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Actions;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.Domain.Actions;

public class ReactOnCallToActionCommandHandler : IRequestHandler<ReactOnCallToActionCommand, CallToAction>
{
    private readonly ICallToActionsService _callToActionsService;
    private readonly IPatientsService _patientsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFlowMaterialization _materializeFlow;

    public ReactOnCallToActionCommandHandler(
        ICallToActionsService callToActionsService, 
        IPatientsService patientsService, 
        IDateTimeProvider dateTimeProvider, 
        IFlowMaterialization materializeFlow)
    {
        _callToActionsService = callToActionsService;
        _patientsService = patientsService;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
    }

    public async Task<CallToAction> Handle(ReactOnCallToActionCommand command, CancellationToken cancellationToken)
    {
        var action = await _callToActionsService.GetAsync(command.Id);
        var specification = PatientSpecifications.Empty;
        var patient = await _patientsService.GetByIdAsync(action.PatientId, specification);

        var flow = new ReactOnCallToActionFlow(
            Patient: patient,
            CallToAction: action,
            ReactionType: command.ReactionType,
            ReactionDetails: command.Details,
            UtcNow: _dateTimeProvider.UtcNow()
        );

        await flow.Materialize(_materializeFlow.Materialize);

        return action;
    }
}