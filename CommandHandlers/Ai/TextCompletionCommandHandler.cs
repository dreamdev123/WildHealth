using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Ai;
using WildHealth.Common.Enums;
using WildHealth.Jenny.Clients.Extensions;
using WildHealth.Jenny.Clients.WebClients;
using WildHealth.Jenny.Clients.Models;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Ai;

public class TextCompletionCommandHandler : IRequestHandler<TextCompletionCommand, TextCompletionResponseModel>
{
    private readonly IJennyConversationWebClient _jennyConversationWebClient;
    
    public TextCompletionCommandHandler(
        IJennyConversationWebClient jennyConversationWebClient
    )
    {
        _jennyConversationWebClient = jennyConversationWebClient;
    }

    public async Task<TextCompletionResponseModel> Handle(TextCompletionCommand command, CancellationToken cancellationToken)
    {
        var requestModel = command
            .FlowTypeModel
            .ToJennyRequestModel(
                userId: command.UserId, 
                authorId: command.AuthorId
            );
        
        return command.FlowType switch
        {
            FlowType.Regular => await requestModel.Execute(_jennyConversationWebClient),
            FlowType.Asynchronous => await requestModel.ExecuteAsync(_jennyConversationWebClient),
            _ => throw new ArgumentException(nameof(FlowType))
        };
    }
}