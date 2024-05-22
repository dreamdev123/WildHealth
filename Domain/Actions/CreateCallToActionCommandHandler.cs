using System.Threading;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Actions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Patients;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.Domain.Actions;

public class CreateCallToActionCommandHandler : IRequestHandler<CreateCallToActionCommand, CallToAction>
{
    private readonly IPatientsService _patientsService;
    private readonly MaterializeFlow _materialize;

    public CreateCallToActionCommandHandler(
        IPatientsService patientsService, 
        MaterializeFlow materialize)
    {
        _patientsService = patientsService;
        _materialize = materialize;
    }

    public async Task<CallToAction> Handle(CreateCallToActionCommand command, CancellationToken cancellationToken)
    {
        var specification = PatientSpecifications.Empty;
        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);

        var flow = new CreateCallToActionFlow(
            Patient: patient,
            Type: command.Type,
            Reactions: command.Reactions,
            ExpiresAt: command.ExpiresAt,
            Data: command.Data
        );

        var result = await flow.Materialize(_materialize).Select<CallToAction>();

        return result;
    }
}