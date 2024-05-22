using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Conversations.Flows;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Conversations;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations;

public class UpdateConversationsSettingsCommandHandler : IRequestHandler<UpdateConversationsSettingsCommand, ConversationsSettings>
{
    private readonly IConversationAwayMessageTemplatesService _awayMessageTemplatesService;
    private readonly IMediator _mediator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializeFlow;

    public UpdateConversationsSettingsCommandHandler(
        IConversationAwayMessageTemplatesService awayMessageTemplatesService,
        IMediator mediator,
        IDateTimeProvider dateTimeProvider, 
        MaterializeFlow materializeFlow)
    {
        _awayMessageTemplatesService = awayMessageTemplatesService;
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
    }

    public async Task<ConversationsSettings> Handle(UpdateConversationsSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await _mediator.Send(new GetOrCreateConversationsSettingsCommand(command.EmployeeId), cancellationToken);

        var messageTemplate = command.AwayMessageEnabled
            ? await _awayMessageTemplatesService.GetById(command.AwayMessageTemplateId ?? 0)
            : null;

        var flow = new UpdateConversationsSettingsFlow(
            settings: settings,
            awayMessageEnabled: command.AwayMessageEnabled,
            awayMessageEnabledFrom: command.AwayMessageEnabledFrom,
            awayMessageEnabledTo: command.AwayMessageEnabledTo,
            awayMessageTemplate: messageTemplate,
            forwardEmployeeEnabled: command.MessageForwardingEnabled,
            forwardEmployeeId: command.MessageForwardingToEmployeeId,
            now: _dateTimeProvider.UtcNow()
        );
        
        settings = await flow.Materialize(_materializeFlow).Select<ConversationsSettings>();

        return settings;
    }
}