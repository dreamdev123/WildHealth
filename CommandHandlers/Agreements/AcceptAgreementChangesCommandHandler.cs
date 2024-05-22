using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Agreements;
using WildHealth.Application.Services.Agreements;
using WildHealth.Application.CommandHandlers.Agreements.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Agreements;

public class AcceptAgreementChangesCommandHandler : IRequestHandler<AcceptAgreementChangesCommand>
{
    private readonly IAgreementsService _agreementsService;
    private readonly MaterializeFlow _materializeFlow;

    public AcceptAgreementChangesCommandHandler(
        IAgreementsService agreementsService, 
        MaterializeFlow materializeFlow)
    {
        _agreementsService = agreementsService;
        _materializeFlow = materializeFlow;
    }

    public async Task Handle(AcceptAgreementChangesCommand command, CancellationToken cancellationToken)
    {
        var confirmations = await _agreementsService.GetPatientConfirmationsAsync(command.PatientId);

        var flow = new AcceptAgreementChangesFlow(command.AgreementId, confirmations);

        await flow.Materialize(_materializeFlow);
    }
}