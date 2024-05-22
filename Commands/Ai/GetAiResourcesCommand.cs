using MediatR;
using WildHealth.Common.Models.Ai;

namespace WildHealth.Application.Commands.Ai;

public class GetAiResourcesCommand : IRequest<AiResourceModel[]>
{
    public GetAiResourceModel[] Models { get; }

    public GetAiResourcesCommand(GetAiResourceModel[] models)
    {
        Models = models;
    }
}