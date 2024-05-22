using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Timezones;
using WildHealth.Application.Services.Schedulers.Accounts;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Utils.Timezones;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace WildHealth.Application.CommandHandlers.Timezones
{
    public class GetCurrentTimezoneCommandHandler : IRequestHandler<GetCurrentTimezoneCommand, TimeZoneInfo>
    {
        private readonly IGeneralRepository<Patient> _patientsRepository;
        private readonly IGeneralRepository<Employee> _employeesRepository;
        private readonly ISchedulerAccountService _schedulerAccountService;
        private readonly IPermissionsGuard _permissionsGuard;

        public GetCurrentTimezoneCommandHandler(
            IGeneralRepository<Patient> patientsRepository,
            IGeneralRepository<Employee> employeesRepository,
            ISchedulerAccountService schedulerAccountService,
            IPermissionsGuard permissionsGuard)
        {
            _patientsRepository = patientsRepository;
            _employeesRepository = employeesRepository;
            _schedulerAccountService = schedulerAccountService;
            _permissionsGuard = permissionsGuard;
        }

        public async Task<TimeZoneInfo> Handle(GetCurrentTimezoneCommand command, CancellationToken cancellationToken)
        {
            return await GetEmployeeTimeZone(command);
        }

        #region private

        private async Task<TimeZoneInfo> GetEmployeeTimeZone(GetCurrentTimezoneCommand command)
        {
            if (command.EmployeeId.HasValue)
            {
                return await GetEmployeeTimeZone(
                    employeeId: command.EmployeeId.Value,
                    practiceId: command.PracticeId
                );
            }

            if (command.PatientId.HasValue)
            {
                return await GetPatientTimeZone(
                    patientId: command.PatientId.Value,
                    practiceId: command.PracticeId
                );
            }

            throw new ArgumentException("Unsupported user type");
        }

        private async Task<TimeZoneInfo> GetEmployeeTimeZone(int employeeId, int practiceId)
        {
            var employee = await _employeesRepository
                .All()
                .ById(employeeId)
                .RelatedToPractice(practiceId)
                .IncludeUser()
                .FirstOrDefaultAsync();

            if (employee is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(employeeId), employeeId);
                throw new AppException(HttpStatusCode.NotFound, "Employee does not exist.", exceptionParam);
            }

            var account = await _schedulerAccountService.GetAccountAsync(employee);
            
            return TimezoneHelper.ToTimezoneInfo(account.Timezone);
        }

        private async Task<TimeZoneInfo> GetPatientTimeZone(int patientId, int practiceId)
        {
            var patient = await _patientsRepository
                .All()
                .ById(patientId)
                .RelatedToPractice(practiceId)
                .IncludeUser()
                .FirstOrDefaultAsync();

            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist.", exceptionParam);
            }

            _permissionsGuard.AssertPermissions(patient);
            
            return TimeZoneInfo.FindSystemTimeZoneById(patient.TimeZone);
        }



        #endregion
    }
}