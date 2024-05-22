using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.AdminTools.Flows;
using WildHealth.Application.Commands.AdminTools;
using WildHealth.Application.Services.AdminTools;
using WildHealth.Application.Services.AdminTools.Helpers;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.InternalMessagesService;
using WildHealth.Domain.Entities.Messages;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.AdminTools;

public class CreateInternalMessageCommandHandler : IRequestHandler<CreateInternalMessageCommand, ICollection<Message>>
{
    private readonly IEmployeeService _employeeService;
    private readonly IInternalMessagesService _internalMessagesService;
    private readonly IMessageSenderService _messageSender;

    public CreateInternalMessageCommandHandler(
        IEmployeeService employeeService,
        IInternalMessagesService internalMessagesService, 
        IMessageSenderService messageSender)
    {
        _employeeService = employeeService;
        _internalMessagesService = internalMessagesService;
        _messageSender = messageSender;
    }

    public async Task<ICollection<Message>> Handle(CreateInternalMessageCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(request.EmployeeId, EmployeeSpecifications.WithUser);

        var flow = new SendMarketingMessageFlow(
            request.Subject,
            request.Body,
            request.Types,
            request.FilterModel,
            employee);

        var result = flow.Execute();

        foreach (var message in result.MessagesToSend)
        {
            await _internalMessagesService.CreateMessageAsync(message);
        }

        result.MessagesToSend.ForEach(m => _messageSender.Send(m, request.FilterModel));

        return result.MessagesToSend;
    }
}