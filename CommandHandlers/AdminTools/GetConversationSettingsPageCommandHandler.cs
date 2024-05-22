using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Application.Commands.AdminTools;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Common.Models.AdminTools;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.CommandHandlers.AdminTools;

public class GetConversationSettingsPageCommandHandler: IRequestHandler<GetConversationSettingsPageCommand,(int, ICollection<ConversationSettingsModel>)>
{
    private readonly IConversationsSettingsService _conversationsSettingsService;
    private readonly IEmployeeService _employeeService;

    public GetConversationSettingsPageCommandHandler(
        IConversationsSettingsService conversationsSettingsService, 
        IEmployeeService employeeService)
    {
        _conversationsSettingsService = conversationsSettingsService;
        _employeeService = employeeService;
    }

    public async Task<(int, ICollection<ConversationSettingsModel>)> Handle(GetConversationSettingsPageCommand request, CancellationToken cancellationToken)
    {
        var (totalCount, conversationSettings) = await _conversationsSettingsService.GetPageAsync(
            page: request.Page,
            pageSize: request.PageSize,
            searchQuery: request.SearchQuery,
            sortingDirection: request.SortingDirection
        );

        var result = new List<ConversationSettingsModel>();
        
        foreach (var settings in conversationSettings)
        {
            var forwardedToEmployee = await _employeeService.GetByIdAsync(settings.ForwardEmployeeId, EmployeeSpecifications.WithUser);

            result.Add(new ConversationSettingsDomain(settings).ToModel(forwardedToEmployee));
        }

        return (totalCount, result);
    }
}