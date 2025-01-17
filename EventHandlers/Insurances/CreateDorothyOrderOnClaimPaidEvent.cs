using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Events.Insurances;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.EventHandlers.Insurances;

public class CreateDorothyOrderOnClaimPaidEvent : INotificationHandler<ClaimPaidEvent>
{
    private readonly IMediator _mediator;
    private readonly IOptions<PracticeOptions> _options;

    public CreateDorothyOrderOnClaimPaidEvent(IMediator mediator, IOptions<PracticeOptions> options)
    {
        _mediator = mediator;
        _options = options;
    }
    
    public async Task Handle(ClaimPaidEvent notification, CancellationToken cancellationToken)
    {
        if (notification.PracticeId == _options.Value.MurrayMedicalId)
        {
            var command = new CreateDorothyOrderCommand(notification.ClaimId);

            await _mediator.Send(command);
        }
    }
}