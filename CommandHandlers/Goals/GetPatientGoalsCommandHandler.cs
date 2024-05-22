using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using WildHealth.Application.Commands.Goals;
using WildHealth.Application.Services.Goals;
using WildHealth.Application.Services.Notes;
using WildHealth.Common.Models.Goals;

namespace WildHealth.Application.CommandHandlers.Goals;

public class GetPatientGoalsCommandHandler : IRequestHandler<GetPatientGoalsCommand, List<GoalModel>>
{
    private readonly IGoalsService _goalsService;
    private readonly INoteService _notesService;
    private readonly IMapper _mapper;

    public GetPatientGoalsCommandHandler(
        IGoalsService goalsService, 
        INoteService notesService,
        IMapper mapper)
    {
        _goalsService = goalsService;
        _notesService = notesService;
        _mapper = mapper;
    }

    public async Task<List<GoalModel>> Handle(GetPatientGoalsCommand command, CancellationToken cancellationToken)
    {
        var result = await _goalsService.GetCurrentAsync(command.PatientId);

        if (!result.Any())
        {
            var altResult = await _notesService.GetPatientGoalsAsync(command.PatientId);
            
            return _mapper.Map<List<GoalModel>>(altResult.Where(x => !x.IsCompleted));
        }
            
        return _mapper.Map<List<GoalModel>>(result);
    }
}
