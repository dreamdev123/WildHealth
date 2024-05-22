using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Ai;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Constants;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Application.Materialization;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Application.CommandHandlers.Ai.Flows;
using WildHealth.Application.Utils.Ai;

namespace WildHealth.Application.CommandHandlers.Ai;

public class CreateAnalyticsLogCommandHandler : IRequestHandler<CreateAnalyticsLogCommand, Unit>
{
    private readonly IEventBus _eventBus;
    private readonly MaterializeFlow _materializeFlow;
    private readonly IAiAnalyticsLoggingParser _parser;
    public CreateAnalyticsLogCommandHandler(
        IEventBus eventBus,
        MaterializeFlow materializeFlow,
        IAiAnalyticsLoggingParser parser
    )
    {
        _eventBus = eventBus;
        _materializeFlow = materializeFlow;
        _parser = parser;
    }

    public async Task<Unit> Handle(CreateAnalyticsLogCommand command, CancellationToken cancellationToken)
    {
        var loggingModel = command.LoggingModel;
        IMaterialisableFlow flow;
        
        switch(loggingModel.AnalyticsLogType)
        {
            case AiConstants.AnalyticsLogTypes.Requested:
                flow = new SendResponseRequestedEventFlow(loggingModel, _parser);
                break;
            case AiConstants.AnalyticsLogTypes.Generated:
                flow = new SendResponseGeneratedEventFlow(loggingModel, _parser);
                break;
            default:
                var message = $"Invalid AnalyticsLogType: {loggingModel.AnalyticsLogType}. No IntegrationEvent sent";
                throw new AppException(HttpStatusCode.BadRequest, message);
        }

        await flow.Materialize(_materializeFlow);

        return Unit.Value;
    }
}