using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Schedulers.Availability;
using WildHealth.Common.Models.Appointments;
using WildHealth.Common.Models.Employees;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Models.Employees;
using AutoMapper;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Appointments;

public class GetPossibleEmployeesAvailabilityCommandCommandHandler : IRequestHandler<GetPossibleEmployeesAvailabilityCommand, PossiblePatientEmployeesAvailabilityModel>
{
    private const int DefaultCount = 5;

    private readonly ISchedulerAvailabilityService _schedulerAvailabilityService;
    private readonly IPatientsService _patientsService;
    private readonly IEmployeeService _employeeService;
    private readonly IMapper _mapper;

    public GetPossibleEmployeesAvailabilityCommandCommandHandler(
        ISchedulerAvailabilityService schedulerAvailabilityService, 
        IPatientsService patientsService, 
        IEmployeeService employeeService, 
        IMapper mapper)
    {
        _schedulerAvailabilityService = schedulerAvailabilityService;
        _patientsService = patientsService;
        _employeeService = employeeService;
        _mapper = mapper;
    }

    public async Task<PossiblePatientEmployeesAvailabilityModel> Handle(GetPossibleEmployeesAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByIdAsync(request.PatientId);
        var practiceId = patient.User.PracticeId;

        var employees = await GetPossibleEmployeesAsync(patient, request.RoleIds);

        var commonAvailabilityTask = _schedulerAvailabilityService.GetCommonUsersAvailabilityAsync(
            practiceId: practiceId,
            employeeSchedulerIds: employees.Select(x => x.SchedulerAccountId).ToArray(),
            from: request.StartDate,
            to: request.EndDate
        );

        var employeesAvailabilityTask = GetEmployeesAvailability(
            practiceId: practiceId,
            configurationId: request.ConfigurationId,
            employees: employees,
            start: request.StartDate,
            end: request.EndDate
        );

        var tasks = new Task[] {commonAvailabilityTask, employeesAvailabilityTask};

        await Task.WhenAll(tasks);

        return new PossiblePatientEmployeesAvailabilityModel
        {
            Employees = _mapper.Map<EmployeeShortModel[]>(employees),
            Availability = new UsersAvailabilityModel
            {
                Common = commonAvailabilityTask.Result,
                Users = employeesAvailabilityTask.Result
            }
        };
    }

    private async Task<Dictionary<int, SchedulerAvailabilityModel[]>> GetEmployeesAvailability(
        int practiceId,
        int configurationId,
        Employee[] employees,
        DateTime start,
        DateTime end)
    {
        var employeesAvailability = new ConcurrentDictionary<int, SchedulerAvailabilityModel[]>(
            employees.ToDictionary(x => x.GetId(), _ => Array.Empty<SchedulerAvailabilityModel>()));

        var tasks = employees.Select(employee => Task.Run(async () =>
        {
            employeesAvailability[employee.GetId()] = await _schedulerAvailabilityService.GetAvailabilityAsync(
                practiceId: practiceId,
                configurationId: configurationId,
                employees: new [] {employee},
                from: start,
                to: end
            );
        }));

        await Task.WhenAll(tasks);

        return employeesAvailability.ToDictionary(x => x.Key, x => x.Value);
    }
    
    private async Task<Employee[]> GetPossibleEmployeesAsync(Patient patient, int[] roleIds)
    {
        var employees = await _employeeService.GetByRoleIdsAsync(
            practiceId: patient.User.PracticeId,
            locationIds: new[] {patient.LocationId},
            roleIds: roleIds);
        
        return employees
            .Where(x=> !string.IsNullOrEmpty(x.SchedulerAccountId))
            .Where(x=> x.States.Any(s=> s.State.Abbreviation == patient.User.BillingAddress.State 
                || s.State.Name == patient.User.BillingAddress.State))
            .OrderByDescending(x=> EmployeeDomain.Create(x).GetSchedulingFactorPoints())
            .Take(DefaultCount)
            .ToArray();
    }
}