using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Orders.Flows;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Orders.Referral;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Utils.AuthTicket;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders;

public class UpdateReferralOrderCommandHandler : IRequestHandler<UpdateReferralOrderCommand, ReferralOrder>
{
    private readonly IPatientProfileService _patientProfileService;
    private readonly IFlowMaterialization _materializeFlow;
    private readonly IReferralOrdersService _referralOrdersService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmployeeService _employeeService;
    private readonly IAddOnsService _addOnsService;
    private readonly IAuthTicket _authTicket;
    private readonly ILogger _logger;

    public UpdateReferralOrderCommandHandler(
        IPatientProfileService patientProfileService,
        IFlowMaterialization materializeFlow, 
        IReferralOrdersService referralOrdersService, 
        IDateTimeProvider dateTimeProvider,
        IEmployeeService employeeService, 
        IAddOnsService addOnsService, 
        IAuthTicket authTicket,
        ILogger<UpdateReferralOrderCommandHandler> logger)
    {
        _patientProfileService = patientProfileService;
        _materializeFlow = materializeFlow;
        _referralOrdersService = referralOrdersService;
        _dateTimeProvider = dateTimeProvider;
        _employeeService = employeeService;
        _addOnsService = addOnsService;
        _authTicket = authTicket;
        _logger = logger;
    }

    public async Task<ReferralOrder> Handle(UpdateReferralOrderCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Updating Referral order with id: {command.Id} has been started.");

        var order = await _referralOrdersService.GetAsync(command.Id);

        var patient = order.Patient;
        
        var patientProfileLink = await _patientProfileService.GetProfileLink(patient.GetId(), patient.User.PracticeId);

        var employee = command.EmployeeId.HasValue
            ? await _employeeService.GetByIdAsync(command.EmployeeId.Value)
            : null;
        
        var addOnIds = command.Items.Select(x => x.AddOnId).ToArray();
        
        var addOns = await FetchAddOnsAsync(addOnIds, order.Patient.User.PracticeId);

        var date = _dateTimeProvider.UtcNow();
        
        var flow = new UpdateReferralOrderFlow(
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
            .Select<ReferralOrder>();

        _logger.LogInformation($"Updating Referral order with id: {command.Id} has been finished.");

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