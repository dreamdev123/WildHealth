using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.CommandHandlers.Conversations.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class ToggleSmsReminderCommandHandler : IRequestHandler<ToggleSmsReminderCommand, UserSetting>
{
    private readonly IPatientsService _patientsService;
    private readonly MaterializeFlow _materializeFlow;

    public ToggleSmsReminderCommandHandler(IPatientsService patientsService, MaterializeFlow materializeFlow)
    {
        _patientsService = patientsService;
        _materializeFlow = materializeFlow;
    }

    public async Task<UserSetting> Handle(ToggleSmsReminderCommand command, CancellationToken cancellationToken)
    {
        var specification = PatientSpecifications.PatientWithSettings;

        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);

        var flow = new ToggleSmsReminderFlow(patient, command.IsActive);

        var result = await flow.Materialize(_materializeFlow);

        return result.Select<UserSetting>();
    }
}