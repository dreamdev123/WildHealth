using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Commands;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Flows;
using WildHealth.Application.Domain.ConversationAwayMessageTemplates.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;

namespace WildHealth.Application.Domain.ConversationAwayMessageTemplates;

public class UpdateConversationAwayMessageTemplateCommandHandler : IRequestHandler<UpdateConversationAwayMessageTemplateCommand>
{
    private readonly MaterializeFlow _materializer;
    private readonly IConversationAwayMessageTemplatesService _templatesService;

    public UpdateConversationAwayMessageTemplateCommandHandler(
        MaterializeFlow materializer, 
        IConversationAwayMessageTemplatesService templatesService)
    {
        _materializer = materializer;
        _templatesService = templatesService;
    }
    
    public async Task Handle(UpdateConversationAwayMessageTemplateCommand request, CancellationToken cancellationToken)
    {
        var templateToUpdate = await _templatesService.GetById(request.Id);
        
        await new UpdateConversationAwayMessageTemplateFlow(templateToUpdate, request.Title, request.Body).Materialize(_materializer);
    }
}