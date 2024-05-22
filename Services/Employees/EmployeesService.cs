using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Shared.Enums;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Extensions;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries.CustomSql;
using WildHealth.Infrastructure.Data.Queries.CustomSql.Models;
using WildHealth.Common.Models.Employees;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Application.Utils.AppointmentTag;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Reports._Base;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Common.Models.Appointments;
using AutoMapper;

namespace WildHealth.Application.Services.Employees
{
    /// <summary>
    /// <see cref="IEmployeeService"/>
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IGeneralRepository<Employee> _employeesRepository;
        private readonly IGeneralRepository<PatientEmployee> _patientEmployeesRepository;
        private readonly IGeneralRepository<EmployeeLocation> _employeeLocationsRepository;
        private readonly IGeneralRepository<EmployeePermission> _employeePermissionsRepository;
        private readonly ICustomSqlDataRunner _customSqlDataRunner;
        private readonly IGeneralRepository<Appointment> _appointmentsRepository;
        private readonly IGeneralRepository<HealthReport> _healthReportRepository;
        private readonly IAppointmentTagsMapperHelper _appointmentTagsMapperHelper;
        private readonly IMapper _mapper;

        private readonly int _labsUploadedCheckPriorDays = 30;

        public EmployeeService(
            IGeneralRepository<Employee> employeesRepository,
            IGeneralRepository<PatientEmployee> patientEmployeesRepository,
            IGeneralRepository<EmployeeLocation> employeeLocationsRepository,
            IGeneralRepository<EmployeePermission> employeePermissionsRepository,
            ICustomSqlDataRunner customSqlDataRunner,
            IGeneralRepository<Appointment> appointmentsRepository,
            IGeneralRepository<HealthReport> healthReportRepository,
            IAppointmentTagsMapperHelper appointmentTagsMapperHelper,
            IMapper mapper)
        {
            _employeesRepository = employeesRepository;
            _patientEmployeesRepository = patientEmployeesRepository;
            _employeeLocationsRepository = employeeLocationsRepository;
            _employeePermissionsRepository = employeePermissionsRepository;
            _customSqlDataRunner = customSqlDataRunner;
            _appointmentsRepository = appointmentsRepository;
            _healthReportRepository = healthReportRepository;
            _mapper = mapper;
            _appointmentTagsMapperHelper = appointmentTagsMapperHelper;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetEmployeesAsync(string, int, int[], int[])"/>
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="practiceId"></param>
        /// <param name="locationIds"></param>
        /// <param name="roleIds"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Employee>> GetEmployeesAsync(string? searchQuery,
            int practiceId,
            int[]? locationIds,
            int[] roleIds)
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .GetByRoles(roleIds)
                .BySearchQuery(searchQuery)
                .RelatedToPractice(practiceId)
                .RelatedToLocations(locationIds)
                .IncludeLocation()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .OrderByUserName()
                .AsNoTracking()
                .ToArrayAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetAllPracticeEmployeesAsync(int)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Employee>> GetAllPracticeEmployeesAsync(int practiceId)
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .RelatedToPractice(practiceId)
                .IncludeLocation()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .OrderByUserName()
                .AsNoTracking()
                .ToArrayAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetAllActiveEmployeesAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Employee>> GetAllActiveEmployeesAsync()
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .IncludeUser()
                .IncludeUserAttachments()
                .OrderByUserName()
                .AsNoTracking()
                .ToArrayAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetEmployeesByPermissionsAsync"/>
        /// </summary>
        /// <param name="permissions"></param>
        /// <param name="practiceId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Employee>> GetEmployeesByPermissionsAsync(PermissionType[] permissions, int practiceId, int locationId)
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .RelatedToPractice(practiceId)
                .GetByPermissions(permissions)
                .RelatedToLocation(new[] { locationId })
                .IncludePermissions()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .OrderByUserName()
                .AsNoTracking()
                .ToArrayAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetCoachesAndProvidersAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="locationIds"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Employee>> GetCoachesAndProvidersAsync(int practiceId, int[] locationIds)
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .RelatedToPractice(practiceId)
                .RelatedToLocation(locationIds)
                .GetByPermission(PermissionType.Coaching)
                .IncludePermissions()
                .IncludeLocation()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .OrderByUserName()
                .AsNoTracking()
                .ToArrayAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetByRoleIdsAsync(int,int[],int[])"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="locationIds"></param>
        /// <param name="roleIds"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Employee>> GetByRoleIdsAsync(int practiceId, int[] locationIds, int[] roleIds)
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .RelatedToPractice(practiceId)
                .RelatedToLocation(locationIds)
                .GetByRoles(roleIds)
                .IncludePatients()
                .IncludePermissions()
                .IncludeLocation()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .IncludeStates()
                .IncludeIntegrations<Employee, EmployeeIntegration>()
                .OrderByUserName()
                .AsNoTracking()
                .ToArrayAsync();

            return employee;
        }

        public async Task<IEnumerable<Employee>> GetByRolesIdsAsync(params int[] roleIds)
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .GetByRoles(roleIds)
                .IncludeUser()
                .IncludeUserAttachments()
                .AsNoTracking()
                .ToArrayAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetActiveAsync(IEnumerable{int}, int, int)"/>
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="practiceId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Employee>> GetActiveAsync(IEnumerable<int> ids, int practiceId, int locationId)
        {
            return await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .RelatedToPractice(practiceId)
                .RelatedToLocation(new[] { locationId })
                .Where(x=> ids.Contains(x.Id!.Value))
                .IncludeLocation()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeIntegrations<Employee, EmployeeIntegration>()
                .AsNoTracking()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetByIntegrationIdAsync"/>
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <param name="practiceId"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public async Task<Employee?> GetByIntegrationIdAsync(string integrationId,
            IntegrationVendor vendor,
            string purpose,
            int practiceId,
            int locationId)
        {
            return await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .ByIntegrationId<Employee, EmployeeIntegration>(integrationId, vendor, purpose)
                .RelatedToPractice(practiceId)
                .RelatedToLocation(new[] { locationId })
                .IncludeLocation()
                .IncludeUser()
                .IncludeUserAttachments()
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetBySchedulerAccountIdAsync(string)"/>
        /// </summary>
        /// <param name="schedulerAccountId"></param>
        /// <returns></returns>
        public async Task<Employee?> GetBySchedulerAccountIdAsync(string schedulerAccountId)
        {
            return await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeLocation()
                .FirstOrDefaultAsync(x => x.SchedulerAccountId == schedulerAccountId);
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetAssignedToAsync(int)"/>
        /// </summary>
        /// <returns></returns>
        public async Task<Employee[]> GetAssignedToAsync(int patientId)
        {
            var employees = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .AssignedToPatient(patientId)
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .IncludeIntegrations<Employee, EmployeeIntegration>()
                .ToArrayAsync();

            return employees;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetByIdAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Employee> GetByIdAsync(int id)
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .GetById(id)
                .IncludeStates()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .IncludeIntegrations<Employee, EmployeeIntegration>()
                .FirstOrDefaultAsync();

            if (employee is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, $"Unable to find active employee for [EmployeeId] = {id}", exceptionParam);
            }

            employee.Patients = await _patientEmployeesRepository
                .All()
                .Where(x => x.EmployeeId == id && !x.DeletedAt.HasValue)
                .Include(x => x.Patient)
                .ToListAsync();

            employee.Locations = await _employeeLocationsRepository
                .All()
                .Where(x => x.EmployeeId == id)
                .Include(x => x.Location)
                .ToListAsync();

            employee.Permissions = await _employeePermissionsRepository
                .All()
                .Where(x => x.EmployeeId == id)
                .ToListAsync();

            return employee;
        }
        
        public async Task<Employee[]> GetEmployeesInfoByIdAsync(int[] ids)
        {
            var employees = await _employeesRepository
                .All()
                .Active()
                .GetById(ids)
                .IncludeUserAttachments()
                .ToArrayAsync();

            return employees;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetByIdsAsync"/>
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public async Task<Employee[]> GetByIdsAsync(int[] ids, ISpecification<Employee> specification)
        {
            if (!ids.Any())
            {
                return Array.Empty<Employee>();
            }
            
            var employees = await _employeesRepository
                .All()
                .GetByIds(ids)
                .ApplySpecification(specification)
                .ToArrayAsync();

            if (!employees.Any())
            {
                throw new AppException(HttpStatusCode.NotFound, $"Unable to find active employees");
            }

            return employees;
        }

        /// <summary>
        /// Returns employee by id with given specification
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public async Task<Employee> GetByIdAsync(int id, ISpecification<Employee> specification)
        {
            var employee = await _employeesRepository
                .All()
                .ById(id)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Unable to find active employees");
            }

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetEmployeeInfoByIdAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Employee> GetEmployeeInfoByIdAsync(int id)
        {
            var employee = await _employeesRepository
                .All()
                .GetById(id)
                .IncludeStates()
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .FirstOrDefaultAsync();

            if (employee is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, $"Unable to find employee for [EmployeeId] = {id}", exceptionParam);
            }

            employee.Patients = await _patientEmployeesRepository
                .All()
                .Where(x => x.EmployeeId == id && !x.DeletedAt.HasValue)
                .Include(x => x.Patient)
                .ToListAsync();

            employee.Locations = await _employeeLocationsRepository
                .All()
                .Where(x => x.EmployeeId == id)
                .Include(x => x.Location)
                .ToListAsync();

            employee.Permissions = await _employeePermissionsRepository
                .All()
                .Where(x => x.EmployeeId == id)
                .ToListAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetByUserIdAsync(int, ISpecification{Employee})"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public async Task<Employee> GetByUserIdAsync(int id, ISpecification<Employee> specification)
        {
            var employee = await _employeesRepository
                .All()
                .GetByUserId(id)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Unable to find active employees");
            }

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.GetByUserIdAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Employee> GetByUserIdAsync(int id)
        {
            var employee = await _employeesRepository
                .All()
                .Active()
                .NotDeleted()
                .GetByUserId(id)
                .IncludeUser()
                .IncludeUserAttachments()
                .IncludeRole()
                .FirstOrDefaultAsync();

            if (employee is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Unable to find active employee", exceptionParam);
            }
            
            employee.Patients = await _patientEmployeesRepository
                .All()
                .Where(x => x.EmployeeId == employee.Id && !x.DeletedAt.HasValue)
                .Include(x => x.Patient)
                .ToArrayAsync();

            employee.Locations = await _employeeLocationsRepository
                .All()
                .Where(x => x.EmployeeId == employee.Id)
                .Include(x => x.Location)
                .ToArrayAsync();

            employee.Permissions = await _employeePermissionsRepository
                .All()
                .Where(x => x.EmployeeId == employee.Id)
                .ToArrayAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.CreateAsync(Employee)"/>
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        public async Task<Employee> CreateAsync(Employee employee)
        {
            await _employeesRepository.AddAsync(employee);

            await _employeesRepository.SaveAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.UpdateAsync(Employee)"/>
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        public async Task<Employee> UpdateAsync(Employee employee)
        {
            _employeesRepository.Edit(employee);

            await _employeesRepository.SaveAsync();

            return employee;
        }

        /// <summary>
        /// <see cref="IEmployeeService.DeleteAsync(Employee)"/>
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        public async Task DeleteAsync(Employee employee)
        {
            // Employee is soft deletable entity.
            // It will market as deleted and still be present in the database
            // In order to limit data viewing need to remove related entities:
            //      * Assigned patients
            //      * Assigned locations
            _employeesRepository.Delete(employee);

            // Delete all patient assigment
            foreach (var patientAssigment in employee.Patients)
            {
                _patientEmployeesRepository.Delete(patientAssigment);
            }
            
            // Delete all location assigment
            foreach (var locationAssigment in employee.Locations)
            {
                _employeesRepository.DeleteRelated(locationAssigment);
            }

            await _employeesRepository.SaveAsync();
        }

        public async Task<EmployeeDashboardModel> GetEmployeeDashboardModel(int employeeId)
        {   
            var result = new EmployeeDashboardModel();

            var employee = await GetByIdAsync(employeeId, EmployeeSpecifications.WithUser);

            var employeeDashboardUnreadMessageModel = await GetEmployeeDashboardUnreadMessageModel(employeeId);
            
            // Get unread messages
            result.EmployeeId = employeeDashboardUnreadMessageModel.EmployeeId;
            result.TotalUnreadMessages = employeeDashboardUnreadMessageModel.TotalUnreadMessages;
            
            // Get unpublished health reports - TotalUnpublishedHealthReports
            // Passing in null as the employeeId here because for health coaches the publishing phase doesn't have a specific employeeId specified
            result.UnpublishedHealthReports = await GetReports(null, employeeId, HealthReportStatus.Completed);
            
            // Get unsigned health reports - TotalUnsignedHealthReports
            result.UnsignedHealthReports = await GetReports(employeeId, employeeId, HealthReportStatus.UnderReview);
            
            // Get upcoming appointments - UpcomingAppointments
            result.UpcomingAppointments = _appointmentTagsMapperHelper.MapAppointmentWithTags(await _appointmentsRepository
                .All()
                .ByEmployeeId(employeeId)
                .ByStatus(AppointmentStatus.Submitted)
                .ByDateRange(DateTime.UtcNow, DateTime.UtcNow.AddDays(1))
                .IncludePatient()
                .IncludePatientWithLabs()
                .IncludePatientSubscription()
                .IncludeEmployee()
                .Sort(Sorting.SortingSource.Appointments.StartDate,Sorting.SortingDirection.Asc)
                .AsNoTracking()
                .ToListAsync());
            
            // Get Recently created appointments - RecentlyAppointments
            // last 24 hs.
            result.RecentlyAddedAppointments = _appointmentTagsMapperHelper.MapAppointmentWithTags(await _appointmentsRepository
                .All()
                .ByEmployeeId(employeeId)
                .ByStatus(AppointmentStatus.Submitted)
                .ByDateCreatedRange(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow )
                .IncludePatient()
                .IncludePatientWithLabs()
                .IncludePatientSubscription()
                .IncludeEmployee()
                .Sort(Sorting.SortingSource.Appointments.StartDate,Sorting.SortingDirection.Asc)
                .AsNoTracking()
                .ToListAsync());
            
            // Get upcoming appointments - upcoming provider appointment and labs haven’t been uploaded in the past 30 days
            result.RecentlyNoUploadedLabFileAppointments = await GetAppointmentWithoutLabInputValue(employeeId);
            
            // Get recently assigns patients - RecentlyAssignedPatients
            result.RecentlyAssignedPatients = _mapper.Map<IEnumerable<EmployeeDashboardPatientModel>>(await GetEmployeeDashboardRecentlyAssignedPatients(
                employeeId: employeeId, 
                since: DateTime.UtcNow.AddDays(-1)));
            
            // Get unassigned patients - UnassignedPatients
            result.UnassignedPatients = _mapper.Map<IEnumerable<EmployeeDashboardPatientModel>>(await GetEmployeeDashboardUnassignedPatients(employee.User.PracticeId));
            
            // Get unverified patients - UnverifiedInsurances
            result.UnverifiedInsurances = _mapper.Map<IEnumerable<EmployeeDashboardUnverifiedInsurance>>(await GetEmployeeDashboardUnverifiedInsurances(employee.User.PracticeId));
            
            return result;
        }

        private async Task<HealthReportDashboardModel[]> GetReports(int? reviewerEmployeeId, int employeeId, HealthReportStatus status)
        {

            var results = await _healthReportRepository
                .All()
                .IncludePatientAndEmployees()
                .ByAssignedEmployee(employeeId)
                .ReviewingBy(reviewerEmployeeId)
                .ByStatus(status)
                .Include(o => o.Patient).ThenInclude(o => o.Appointments)
                .OrderBy(x => x.Status.Date)
                .AsNoTracking()
                .ToArrayAsync();

            return _mapper.Map<HealthReportDashboardModel[]>(results);
        }
        
        private async Task<EmployeeDashboardUnreadMessageModel> GetEmployeeDashboardUnreadMessageModel(int employeeId) {

            const string queryPath = "Queries/CustomSql/Sql/EmployeeDashboardUnreadMessagesQuery.sql";
            var parameters = new List<CustomSqlDataParameter>
            {
                new ()
                {
                    ParameterName = "@employeeId",
                    ParameterValue = employeeId.ToString(),
                    DbType = DbType.String
                }
            };

            var result = await _customSqlDataRunner.GetSingle<EmployeeDashboardUnreadMessageModel>(queryPath, parameters) 
                 ?? new EmployeeDashboardUnreadMessageModel
                    {
                        EmployeeId = employeeId,
                        TotalUnreadMessages = 0
                    };

            return result;
        }

        private async Task<IEnumerable<EmployeeDashboardPatientModelRaw>> GetEmployeeDashboardRecentlyAssignedPatients(int employeeId, DateTime since)
        {
            const string queryPath = "Queries/CustomSql/Sql/EmployeeDashboardRecentlyAssignedPatients.sql";
            var parameters = new List<CustomSqlDataParameter>
            {
                new ()
                {
                    ParameterName = "@employeeId",
                    ParameterValue = employeeId.ToString(),
                    DbType = DbType.String
                },
                new ()
                {
                    ParameterName = "@since",
                    ParameterValue = since,
                    DbType = DbType.DateTime
                }
            };

            return await _customSqlDataRunner.GetDataSet<EmployeeDashboardPatientModelRaw>(queryPath, parameters);
        }
        
        private async Task<IEnumerable<EmployeeDashboardPatientModelRaw>> GetEmployeeDashboardUnassignedPatients(int practiceId)
        {
            const string queryPath = "Queries/CustomSql/Sql/EmployeeDashboardUnassignedPatients.sql";
            var parameters = new List<CustomSqlDataParameter>
            {
                new ()
                {
                    ParameterName = "@practiceId",
                    ParameterValue = practiceId.ToString()
                }
            };

            return await _customSqlDataRunner.GetDataSet<EmployeeDashboardPatientModelRaw>(queryPath, parameters);
        }
        
        private async Task<IEnumerable<EmployeeDashboardUnverifiedInsuranceRaw>> GetEmployeeDashboardUnverifiedInsurances(int practiceId)
        {
            const string queryPath = "Queries/CustomSql/Sql/EmployeeDashboardUnverifiedInsurances.sql";
            var parameters = new List<CustomSqlDataParameter>
            {
                new ()
                {
                    ParameterName = "@practiceId",
                    ParameterValue = practiceId.ToString()
                }
            };

            return await _customSqlDataRunner.GetDataSet<EmployeeDashboardUnverifiedInsuranceRaw>(queryPath, parameters);
        }

        private async Task<EmployeeAppointmentModel[]> GetAppointmentWithoutLabInputValue(int employeeId)
        {
            var results = await _appointmentsRepository
                .All()
                .ByAssignedEmployeeId(employeeId)
                .ByStatus(AppointmentStatus.Submitted)
                
                // Follow up medical Consult
                .ByTypeWithName(AppointmentTypeNames.FollowUpMedicalConsult)
                
                .IncludePatient()
                .IncludePatientWithLabs()
                .IncludePatientSubscription()
                .IncludeEmployee()
                .Sort(Sorting.SortingSource.Appointments.StartDate, Sorting.SortingDirection.Asc)
                
                // Appointments are in the future
                .Where(o => o.StartDate >= DateTime.UtcNow)
                
                // Does not have any lab inputs uploaded within 30 days of the appointment starting
                .Where(o => !o.Patient.InputsAggregator.LabInputValues.Any(x => x.Date >= o.StartDate.AddDays(-_labsUploadedCheckPriorDays)))
                
                // It has not yet been signed off for this appointment that there are no labs 30 days prior to start date
                .Where(o => o.AppointmentSignOffs.All(x => x.SignOffType != AppointmentSignOffType.Labs30DaysPrior))
                
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<EmployeeAppointmentModel[]>(results);
        }
    }
}