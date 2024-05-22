using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Orders.Flows;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Application.Services.Employees;
using WildHealth.Shared.Utils.AuthTicket;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders;

public class CreateOtherOrderCommandHandler : IRequestHandler<CreateOtherOrderCommand, OtherOrder>
{
    private readonly IPatientProfileService _patientProfileService;
    private readonly IFlowMaterialization _materializeFlow;
    private readonly IEmployeeService _employeeService;
    private readonly IPatientsService _patientsService;
    private readonly IAddOnsService _addOnsService;
    private readonly IAuthTicket _authTicket;
    private readonly ILogger _logger;

    public CreateOtherOrderCommandHandler(
        IPatientProfileService patientProfileService,
        IFlowMaterialization materializeFlow,
        IEmployeeService employeeService,
        IPatientsService patientsService,
        IAddOnsService addOnsService,
        IAuthTicket authTicket,
        ILogger<CreateOtherOrderCommandHandler> logger)
    {
        _patientProfileService = patientProfileService;
        _materializeFlow = materializeFlow;
        _employeeService = employeeService;
        _patientsService = patientsService;
        _addOnsService = addOnsService;
        _authTicket = authTicket;
        _logger = logger;
    }

    public async Task<OtherOrder> Handle(CreateOtherOrderCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Creating Other order for patient with id: {command.PatientId} has been started.");

        var date = DateTime.UtcNow;

        var patient = await _patientsService.GetByIdAsync(command.PatientId);

        var patientProfileLink = await _patientProfileService.GetProfileLink(patient.GetId(), patient.User.PracticeId);

        var employee = await _employeeService.GetByIdAsync(command.EmployeeId);
        
        var addOnIds = command.Items.Select(x => x.AddOnId).ToArray();
        
        var addOns = await FetchAddOnsAsync(addOnIds, patient.User.PracticeId);

        var flow = new CreateOtherOrderFlow(
            patientProfileLink: patientProfileLink,
            sendForReview: command.SendForReview,
            isCompleted: command.IsCompleted,
            itemModels: command.Items,
            dataModel: command.Data,
            employee: employee,
            patient: patient,
            addOns: addOns,
            employeeId: _authTicket.GetEmployeeId() ?? 0,
            utcNow: date
        );
        
        var order = await flow
            .Materialize(_materializeFlow.Materialize)
            .Select<OtherOrder>();
        
        _logger.LogInformation($"Creating Other order for patient with id: {command.PatientId} has been finished.");

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