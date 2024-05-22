using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Orders.Other;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Application.CommandHandlers.Orders.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Utils.AuthTicket;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders;

public class UpdateOtherOrderCommandHandler : IRequestHandler<UpdateOtherOrderCommand, OtherOrder>
{
    private readonly IPatientProfileService _patientProfileService;
    private readonly IFlowMaterialization _materializeFlow;
    private readonly IOtherOrdersService _otherOrdersService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmployeeService _employeeService;
    private readonly IAddOnsService _addOnsService;
    private readonly IAuthTicket _authTicket;
    private readonly ILogger _logger;

    public UpdateOtherOrderCommandHandler(
        IPatientProfileService patientProfileService,
        IFlowMaterialization materializeFlow, 
        IOtherOrdersService otherOrdersService, 
        IDateTimeProvider dateTimeProvider,
        IEmployeeService employeeService, 
        IAddOnsService addOnsService, 
        IAuthTicket authTicket,
        ILogger<UpdateOtherOrderCommandHandler> logger)
    {
        _patientProfileService = patientProfileService;
        _materializeFlow = materializeFlow;
        _otherOrdersService = otherOrdersService;
        _dateTimeProvider = dateTimeProvider;
        _employeeService = employeeService;
        _addOnsService = addOnsService;
        _authTicket = authTicket;
        _logger = logger;
    }

    public async Task<OtherOrder> Handle(UpdateOtherOrderCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Updating Other order with id: {command.Id} has been started.");

        var order = await _otherOrdersService.GetAsync(command.Id);

        var patient = order.Patient;
        
        var patientProfileLink = await _patientProfileService.GetProfileLink(patient.GetId(), patient.User.PracticeId);
        
        var employee = await _employeeService.GetByIdAsync(command.EmployeeId);
        
        var addOnIds = command.Items.Select(x => x.AddOnId).ToArray();
        
        var addOns = await FetchAddOnsAsync(addOnIds, order.Patient.User.PracticeId);
        
        var date = _dateTimeProvider.UtcNow();
        
        var flow = new UpdateOtherOrderFlow(
            employeeId: _authTicket.GetEmployeeId() ?? 0,
            patientProfileLink: patientProfileLink,
            sendForReview: command.SendForReview,
            isCompleted: command.IsCompleted,
            itemModels: command.Items,
            dataModel: command.Data,
            employee: employee,
            addOns: addOns,
            order: order,
            utcNow: date
        );
        
        order = await flow
            .Materialize(_materializeFlow.Materialize)
            .Select<OtherOrder>();

        _logger.LogInformation($"Updating Other order with id: {command.Id} has been finished.");

        return order;
    }
    
    #region private

    /// <summary>
    /// Fetches and returns add-ons by ids
    /// </summary>
    /// <param name="addOnIds"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    private async Task<AddOn[]> FetchAddOnsAsync(int[] addOnIds, int practiceId)
    {
        var addOns = await _addOnsService.GetByIdsAsync(addOnIds, practiceId);

        return addOns.ToArray();
    }
    
    #endregion
}