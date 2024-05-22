using System.Collections.Generic;
using MediatR;
using WildHealth.Common.Models.AdminTools;

namespace WildHealth.Application.Commands.AdminTools;

public class GetConversationSettingsPageCommand: IRequest<(int, ICollection<ConversationSettingsModel>)>
{
    public int Page { get; }
    
    public int PageSize { get; }

    public string? SearchQuery { get; }

    public string SortingDirection { get; }

    public GetConversationSettingsPageCommand(int page, int pageSize, string? searchQuery, string sortingDirection)
    {
        Page = page;
        PageSize = pageSize;
        SearchQuery = searchQuery;
        SortingDirection = sortingDirection;
    }
}