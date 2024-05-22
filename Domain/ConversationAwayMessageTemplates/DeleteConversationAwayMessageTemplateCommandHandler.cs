using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Commands;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Flows;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates;

public class DeleteConversationAwayMessageTemplateCommandHandler : IRequestHandler<DeleteConversationAwayMessageTemplateCommand>
{
    private readonly MaterializeFlow _materializer;
    private readonly IConversationAwayMessageTemplatesService _templatesService;

    public DeleteConversationAwayMessageTemplateCommandHandler(
        MaterializeFlow materializer, 
        IConversationAwayMessageTemplatesService templatesService)
    {
        _materializer = materializer;
        _templatesService = templatesService;
    }

    public async Task Handle(DeleteConversationAwayMessageTemplateCommand request, CancellationToken cancellationToken)
    {
        var templateToDelete = await _templatesService.GetById(request.Id);
        
        await new DeleteConversationAwayMessageTemplateFlow(templateToDelete).Materialize(_materializer);
    }
}