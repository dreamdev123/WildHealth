using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.PatientEngagements;
using WildHealth.Application.Domain.PatientEngagements.Flows;
using WildHealth.Application.Extensions.Query;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Common.Models.HealthCoachEngagement;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Application.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Utils.DateTimes;

namespace WildHealth.Application.Domain.PatientEngagements.CommandHandlers;

public class CompleteHealthCoachEngagementCommandHandler: IRequestHandler<CompleteHealthCoachEngagementCommand, HealthCoachEngagementTaskModel>
{
    private readonly IGeneralRepository<PatientEngagement> _engagementRepository;
    private readonly IUsersService _userService;
    private readonly MaterializeFlow _materialize;
    private readonly IDateTimeProvider _dateTimeProvider;
    
    public CompleteHealthCoachEngagementCommandHandler(
        IGeneralRepository<PatientEngagement> engagementRepository, 
        IUsersService userService, 
        MaterializeFlow materialize, 
        IDateTimeProvider dateTimeProvider)
    {
        _engagementRepository = engagementRepository;
        _userService = userService;
        _materialize = materialize;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<HealthCoachEngagementTaskModel> Handle(CompleteHealthCoachEngagementCommand command, CancellationToken cancellationToken)
    {
        var completedBy = await _userService.GetByIdAsync(command.UserId);
        var task = await _engagementRepository
            .All()
            .Include(pe => pe.EngagementCriteria)
            .Include(pe => pe.Patient)
            .ThenInclude(p => p.User)
            .ById(command.EngagementTaskId)
            .FindAsync();

        var flow = new CompleteHealthCoachEngagementFlow(task, completedBy, _dateTimeProvider.UtcNow());

        await flow.Materialize(_materialize);

        return new HealthCoachEngagementTaskModel
        {
            PatientId = task.PatientId,
            EngagementId = task.Id!.Value,
            PatientFullName = task.Patient.User.FullName(),
            EventName = task.EngagementCriteria.Name
        };
    }
}