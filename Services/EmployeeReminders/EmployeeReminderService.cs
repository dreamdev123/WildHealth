using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.EmployeeReminders;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.EmployeeReminders;

public class EmployeeReminderService : IEmployeeReminderService
{
    private readonly IGeneralRepository<EmployeeReminder> _employeeReminderRepository;

    public EmployeeReminderService(IGeneralRepository<EmployeeReminder> employeeReminderRepository)
    {
        _employeeReminderRepository = employeeReminderRepository;
    }

    /// <summary>
    /// <see cref="IEmployeeReminderService.CreateAsync"/>
    /// </summary>
    /// <param name="employeeReminder"></param>
    /// <returns></returns>
    public async Task<EmployeeReminder> CreateAsync(EmployeeReminder employeeReminder)
    {
        await _employeeReminderRepository.AddAsync(employeeReminder);

        await _employeeReminderRepository.SaveAsync();

        return employeeReminder;
    }

    /// <summary>
    /// <see cref="IEmployeeReminderService.UpdateAsync"/>
    /// </summary>
    /// <param name="employeeReminder"></param>
    /// <returns></returns>
    public async Task<EmployeeReminder> UpdateAsync(EmployeeReminder employeeReminder)
    {
        _employeeReminderRepository.Edit(employeeReminder);

        await _employeeReminderRepository.SaveAsync();

        return employeeReminder;
    }

    /// <summary>
    /// <see cref="IEmployeeReminderService.GetByEmployeeIdAsync"/>
    /// </summary>
    /// <param name="employeeId"></param>
    /// <param name="dateTimeStart"></param>
    /// <param name="dateTimeEnd"></param>
    /// <returns></returns>
    public async Task<ICollection<EmployeeReminder>> GetByEmployeeIdAsync(int employeeId, DateTime dateTimeStart, DateTime dateTimeEnd)
    {
        var result = await _employeeReminderRepository
            .All()
            .ByEmployeeId(employeeId)
            .ByDate(dateTimeStart, dateTimeEnd)
            .ToListAsync();

        return result;
    }

    /// <summary>
    /// <see cref="IEmployeeReminderService.GetByIdAsync"/>
    /// </summary>
    /// <param name="employeeReminderId"></param>
    /// <returns></returns>
    /// <exception cref="AppException"></exception>
    public async Task<EmployeeReminder> GetByIdAsync(int employeeReminderId)
    {
        var result = await _employeeReminderRepository
            .All()
            .ById(employeeReminderId)
            .FirstOrDefaultAsync();

        if (result is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Employee reminder does not exist.");
        }
        
        return result;
    }

    /// <summary>
    /// <see cref="IEmployeeReminderService.DeleteAsync"/>
    /// </summary>
    /// <param name="employeeReminderId"></param>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    public async Task DeleteAsync(int employeeReminderId, int employeeId)
    {
        var employeeReminder = _employeeReminderRepository
            .All()
            .ById(employeeReminderId)
            .ByEmployeeId(employeeId)
            .FirstOrDefault();

        if (employeeReminder is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Employee reminder does not exist.");
        }
        
        _employeeReminderRepository.Delete(employeeReminder);

        await _employeeReminderRepository.SaveAsync();
    }
}