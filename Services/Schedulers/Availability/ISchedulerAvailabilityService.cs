using System;
using System.Threading.Tasks;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Services.Schedulers.Availability;

public interface ISchedulerAvailabilityService
{
    /// <summary>
    /// Returns employee availability
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="configurationId"></param>
    /// <param name="employeeIds"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    Task<SchedulerAvailabilityModel[]> GetAvailabilityAsync(
        int practiceId, 
        int? configurationId,
        int[] employeeIds, 
        DateTime from, 
        DateTime to);

    /// <summary>
    /// Returns employee availability
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="configurationId"></param>
    /// <param name="employees"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    Task<SchedulerAvailabilityModel[]> GetAvailabilityAsync(
        int practiceId, 
        int? configurationId,
        Employee[] employees, 
        DateTime from, 
        DateTime to);

    /// <summary>
    /// Returns possible times for employees 
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="employeeSchedulerIds"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    Task<SchedulerAvailabilityModel[]> GetCommonUsersAvailabilityAsync(
        int practiceId,
        string[] employeeSchedulerIds,
        DateTime from,
        DateTime to);

    /// <summary>
    /// Return amount of employee time slots
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="schedulerAccountId"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    Task<int> GetAvailabilityCountAsync(
        int practiceId,
        string schedulerAccountId,
        DateTime from,
        DateTime to,
        int duration);
}