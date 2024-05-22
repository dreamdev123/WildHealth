using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class GetSupportSubmissionsCommandHandler : IRequestHandler<GetSupportSubmissionsCommand, IEnumerable<Conversation>>
    {
        private readonly IConversationsService _conversationsService;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger _logger;

        public GetSupportSubmissionsCommandHandler(
            IConversationsService conversationsService,
            IEmployeeService employeeService,
            ILogger<GetSupportSubmissionsCommandHandler> logger)
        {
            _conversationsService = conversationsService;
            _employeeService = employeeService;
            _logger = logger;
        }

        public async Task<IEnumerable<Conversation>> Handle(GetSupportSubmissionsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Getting support submissions conversations for employee with [employeeId] {command.EmployeeId} has been started.");

            var employee = await _employeeService.GetByIdAsync(command.EmployeeId);

            var locationIds = employee.Locations.Select(x => x.LocationId).ToArray();

            var conversationsOpenSupportForEmployee = await _conversationsService.GetSupportSubmissionsAsync(locationIds);

            var result = conversationsOpenSupportForEmployee
                .Where(x => x.PracticeId == employee.User.PracticeId && x.State == ConversationState.Active)
                .ToArray();

            _logger.LogInformation($"Getting support submissions conversations for employee with [employeeId] {command.EmployeeId} has been finished.");

            return result;
        }
    }
}
