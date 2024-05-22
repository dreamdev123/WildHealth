using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.CommandHandlers.Inputs.Flow;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Extensions.Query;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Data.Repository;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Inputs;

public class UpdateLabInputHighlightCommandHandler : IRequestHandler<UpdateLabInputHighlightCommand, LabInput>
{
    private readonly IGeneralRepository<LabInput> _labInputRepository;
    private readonly IFlowMaterialization _materialization;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPatientsService _patientsService;

    public UpdateLabInputHighlightCommandHandler(
        IGeneralRepository<LabInput> labInputRepository, 
        IFlowMaterialization materialization,
        IDateTimeProvider dateTimeProvider,
        IPatientsService patientsService)
    {
        _labInputRepository = labInputRepository;
        _dateTimeProvider = dateTimeProvider;
        _materialization = materialization;
        _patientsService = patientsService;
    }

    public async Task<LabInput> Handle(UpdateLabInputHighlightCommand command, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow();
        var specification = PatientSpecifications.Empty;
        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);
        var input = await _labInputRepository
            .All()
            .Include(x => x.Highlight)
            .Include(x => x.Values)
            .Where(o => o.Id == command.Id)
            .FindAsync();

        var flow = new UpdateLabInputHighlightFlow(input, patient, command.IsActive, utcNow);

        await flow.Materialize(_materialization.Materialize);

        return input;
    }
}