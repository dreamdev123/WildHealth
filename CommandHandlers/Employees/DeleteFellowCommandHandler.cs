using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Fellows;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Employees;

public class DeleteFellowCommandHandler: IRequestHandler<DeleteFellowCommand>
{
    private readonly IFellowsService _fellowsService;

    public DeleteFellowCommandHandler(IFellowsService fellowsService)
    {
        _fellowsService = fellowsService;
    }

    public async Task Handle(DeleteFellowCommand command, CancellationToken cancellationToken)
    {
        var fellow = await _fellowsService.GetByIdAsync(command.EmployeeId);

        await _fellowsService.DeleteAsync(fellow);
    }
}