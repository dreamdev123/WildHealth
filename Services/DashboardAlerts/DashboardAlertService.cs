using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Common.Extensions;
using WildHealth.Common.Models._Base;
using WildHealth.Common.Models.DashboardAlerts;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.DashboardAlerts;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.DashboardAlerts;

public class DashboardAlertService : IDashboardAlertService
{
    private readonly IGeneralRepository<DashboardAlert> _dashboardAlertsRepository;
    private readonly IGeneralRepository<Employee> _employeeRepository;
    private readonly IGeneralRepository<PatientEmployee> _patientEmployeeRepository;
    private readonly IGeneralRepository<User> _userRepository;
    private readonly IGeneralRepository<Patient> _patientRepository;

    public DashboardAlertService(
        IGeneralRepository<DashboardAlert> dashboardAlertsRepository, 
        IGeneralRepository<Employee> employeeRepository, 
        IGeneralRepository<PatientEmployee> patientEmployeeRepository, 
        IGeneralRepository<User> userRepository, 
        IGeneralRepository<Patient> patientRepository)
    {
        _dashboardAlertsRepository = dashboardAlertsRepository;
        _employeeRepository = employeeRepository;
        _patientEmployeeRepository = patientEmployeeRepository;
        _userRepository = userRepository;
        _patientRepository = patientRepository;
    }

    public async Task<DashboardAlert> CreateAsync(DashboardAlert alert)
    {
        await _dashboardAlertsRepository.AddAsync(alert);
        await _dashboardAlertsRepository.SaveAsync();

        return alert;
    }

    public async Task<List<DashboardAlert>> GetForEmployeeAsync(int employeeId)
    {
        var result = _employeeRepository
            .All()
            .Where(x => x.Id == employeeId)
            .SelectMany(x => x.Patients)
            .SelectMany(x => x.Patient.DashboardAlerts)
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .NotDeleted();

        return await result
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<DashboardAlert>> GetForLocationsAsync(int[] locationIds)
    {
        var result = _patientRepository
            .All()
            .Where(x => locationIds.Contains(x.LocationId))
            .SelectMany(x => x.DashboardAlerts)
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .NotDeleted();

        return await result
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<PaginationModel<DashboardAlertHistoryModel>> GetHistoryAsync(int employeeId,
        int page,
        int pageSize,
        string? searchName)
    {
        var query = HistoryDashboardAlerts(employeeId);

        return await GetHistoryCore(query, page, pageSize, searchName);
    }
    
    public async Task<PaginationModel<DashboardAlertHistoryModel>> GetHistoryAsync(int[] locationIds,
        int page,
        int pageSize,
        string? searchName)
    {
        var query = HistoryDashboardAlerts(locationIds);

        return await GetHistoryCore(query, page, pageSize, searchName);
    }
    
    private async Task<PaginationModel<DashboardAlertHistoryModel>> GetHistoryCore(
        IQueryable<DashboardAlert> dashboardAlerts,
        int page,
        int pageSize,
        string? searchName)
    {
        var query = dashboardAlerts
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Join(
                _userRepository.All(), 
                a => a.DeletedBy,
                u => u.Id,
                (a, u) => new DashboardAlertHistoryModel
                {
                    Id = a.Id!.Value,
                    Title = a.Title,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt,
                    ResolvedAt = a.DeletedAt!.Value,
                    PatientId = a.PatientId,
                    Patient = new FullName
                    {
                        FirstName = a.Patient.User.FirstName, 
                        LastName = a.Patient.User.LastName
                    },
                    ResolvedBy = new FullName
                    {
                        FirstName = u.FirstName, 
                        LastName = u.LastName
                    }
                }).AsNoTracking();

        if (!string.IsNullOrEmpty(searchName))
        {
            query = query.Where(x => 
                x.Patient.FirstName.Contains(searchName) ||
                x.Patient.LastName.Contains(searchName) ||
                x.ResolvedBy.FirstName.Contains(searchName) ||
                x.ResolvedBy.LastName.Contains(searchName));
        }

        return await query.ToPagedResult(page, pageSize);
    }

    private IQueryable<DashboardAlert> HistoryDashboardAlerts(int employeeId)
    {
        return _employeeRepository
            .All()
            .Where(x => x.Id == employeeId)
            .SelectMany(x => x.Patients)
            .SelectMany(x => x.Patient.DashboardAlerts)
            .Where(x => x.DeletedAt.HasValue)
            .OrderBy(o => o.CreatedAt);
    }
    
    private IQueryable<DashboardAlert> HistoryDashboardAlerts(int[] locationIds)
    {
        return _patientRepository
            .All()
            .Where(x => locationIds.Contains(x.LocationId))
            .SelectMany(x => x.DashboardAlerts)
            .Where(x => x.DeletedAt.HasValue);
    }

    public async Task<bool> IsAssignedToEmployeeAsync(DashboardAlert alert, int employeeId)
    {
        return await _patientEmployeeRepository.ExistsAsync(x => 
            x.PatientId == alert.PatientId &&
            x.EmployeeId == employeeId);
    }

    public async Task<bool> IsAssignedToCareCoordinatorAsync(DashboardAlert alertToResolve, int[] locationIds)
    {
        return await _patientRepository
            .All()
            .Where(x => locationIds.Contains(x.LocationId))
            .SelectMany(x => x.DashboardAlerts)
            .AnyAsync(x => x.Id == alertToResolve.Id);
    }

    public async Task<DashboardAlert> GetByIdAsync(int id)
    {
        return await _dashboardAlertsRepository.GetAsync(id);
    }

    public async Task DeleteAsync(DashboardAlert dashboardAlert)
    {
        _dashboardAlertsRepository.Delete(dashboardAlert);
        await _dashboardAlertsRepository.SaveAsync();
    }
}