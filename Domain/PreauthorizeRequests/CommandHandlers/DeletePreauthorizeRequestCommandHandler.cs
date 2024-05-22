using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Domain.PreauthorizeRequests.Commands;
using WildHealth.Application.Domain.PreauthorizeRequests.Flows;
using WildHealth.Application.Domain.PreauthorizeRequests.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Users;
using MediatR;

namespace WildHealth.Application.Domain.PreauthorizeRequests.CommandHandlers;

public class DeletePreauthorizeRequestCommandHandler : IRequestHandler<DeletePreauthorizeRequestCommand, PreauthorizeRequest>
{
    private readonly IPreauthorizeRequestsService _preauthorizeRequestsService;
    private readonly MaterializeFlow _materialize;

    public DeletePreauthorizeRequestCommandHandler(
        IPreauthorizeRequestsService preauthorizeRequestsService, 
        MaterializeFlow materialize)
    {
        _preauthorizeRequestsService = preauthorizeRequestsService;
        _materialize = materialize;
    }

    public async Task<PreauthorizeRequest> Handle(DeletePreauthorizeRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _preauthorizeRequestsService.GetByIdAsync(command.Id);
        
        var flow = new DeletePreauthorizeRequestFlow(request);
        
        var result = await flow.Materialize(_materialize);

        return result.Select<PreauthorizeRequest>();
    }
}