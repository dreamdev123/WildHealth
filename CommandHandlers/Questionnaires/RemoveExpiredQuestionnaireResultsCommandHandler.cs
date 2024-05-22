using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Questionnaires;
using WildHealth.Application.Services.QuestionnaireResults;

namespace WildHealth.Application.CommandHandlers.Questionnaires;

public class RemoveExpiredQuestionnaireResultsCommandHandler : IRequestHandler<RemoveExpiredQuestionnaireResultsCommand>
{
    private readonly IQuestionnaireResultsService _questionnaireResultsService;

    public RemoveExpiredQuestionnaireResultsCommandHandler(IQuestionnaireResultsService questionnaireResultsService)
    {
        _questionnaireResultsService = questionnaireResultsService;
    }

    public async Task Handle(RemoveExpiredQuestionnaireResultsCommand request, CancellationToken cancellationToken)
    {
        await _questionnaireResultsService.RemoveExpiredAsync();
    }
}