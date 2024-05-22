using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models._Base;
using WildHealth.Common.Models.DashboardAlerts;
using WildHealth.Domain.Entities.DashboardAlerts;

namespace WildHealth.Application.Services.DashboardAlerts;

public interface IDashboardAlertService
{
    Task<DashboardAlert> CreateAsync(DashboardAlert alert);
    Task<List<DashboardAlert>> GetForEmployeeAsync(int employeeId);
    Task<List<DashboardAlert>> GetForLocationsAsync(int[] locationIds);
    Task<bool> IsAssignedToEmployeeAsync(DashboardAlert alert, int employeeId);
    Task<bool> IsAssignedToCareCoordinatorAsync(DashboardAlert alertToResolve, int[] locationIds);
    Task<DashboardAlert> GetByIdAsync(int id);
    Task<PaginationModel<DashboardAlertHistoryModel>> GetHistoryAsync(int employeeId, int page, int pageSize, string? searchName);
    Task<PaginationModel<DashboardAlertHistoryModel>> GetHistoryAsync(int[] locationIds, int page, int pageSize, string? searchName);
    Task DeleteAsync(DashboardAlert dashboardAlert);
}