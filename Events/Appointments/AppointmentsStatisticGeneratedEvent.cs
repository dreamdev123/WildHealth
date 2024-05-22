using System;
using MediatR;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Events.Appointments;

public class AppointmentsStatisticGeneratedEvent : INotification
{
    public AppointmentsStatisticModel AppointmentsStatistic { get; }
    public Employee Employee { get; }
    public DateTime StatisticGenerationDate { get; }
    
    public AppointmentsStatisticGeneratedEvent(AppointmentsStatisticModel appointmentsStatistic, Employee employee, DateTime statisticGenerationDate)
    {
        AppointmentsStatistic = appointmentsStatistic;
        Employee = employee;
        StatisticGenerationDate = statisticGenerationDate;
    }
}