using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.EmployeeReminders;

namespace WildHealth.Application.Services.EmployeeReminders;

public interface IEmployeeReminderService
{
    /// <summary>
    /// Creates reminder for employee
    /// </summary>
    /// <param name="employeeReminder"></param>
    /// <returns></returns>
    public Task<EmployeeReminder> CreateAsync(EmployeeReminder employeeReminder);

    /// <summary>
    /// Update reminder for employee
    /// </summary>
    /// <param name="employeeReminder"></param>
    /// <returns></returns>
    public Task<EmployeeReminder> UpdateAsync(EmployeeReminder employeeReminder);

    /// <summary>
    /// Return reminders for employee
    /// </summary>
    /// <param name="employeeId"></param>
    /// <param name="dateTimeStart"></param>
    /// <param name="dateTimeEnd"></param>
    /// <returns></returns>
    public Task<ICollection<EmployeeReminder>> GetByEmployeeIdAsync(int employeeId, DateTime dateTimeStart, DateTime dateTimeEnd);

    /// <summary>
    /// Return reminder by id
    /// </summary>
    /// <param name="employeeReminderId"></param>
    /// <returns></returns>
    public Task<EmployeeReminder> GetByIdAsync(int employeeReminderId);

    /// <summary>
    /// Remove employee reminder
    /// </summary>
    /// <param name="employeeReminderId"></param>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    public Task DeleteAsync(int employeeReminderId, int employeeId);

}