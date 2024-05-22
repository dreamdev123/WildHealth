using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Patients;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class VerifyInsuranceAppointmentCommandHandler : IRequestHandler<VerifyInsuranceAppointmentsCommand>
{
    private const int DefaultDaysBoost = 1; //days (24 hours)
    
    private readonly IPatientsService _patientsService;
    private readonly IMediator _mediator;
    private readonly ILogger<VerifyInsuranceAppointmentCommandHandler> _logger;

    public VerifyInsuranceAppointmentCommandHandler(
        IPatientsService patientsService, 
        IMediator mediator,
        ILogger<VerifyInsuranceAppointmentCommandHandler> logger)
    {
        _patientsService = patientsService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(VerifyInsuranceAppointmentsCommand command, CancellationToken cancellationToken)
    {
        if (IsSaturday(DateTime.UtcNow.DayOfWeek))
        {
            return;
        }
        
        _logger.LogInformation("Verification of upcoming insurance appointments has: started");

        var to = GetBoostDate(DateTime.UtcNow.Date);
        var from = DateTime.UtcNow.Date.AddDays(-1);

        var patients = await _patientsService.GetInsurancePatientsWithUpcomingAppointment(
            practiceId: command.PracticeId,
            from: from,
            to: to);

        foreach (var patient in patients)
        {
            try
            {
                await _mediator.Send(new RunInsuranceVerificationCommand(patientId: patient.GetId()),
                    cancellationToken);
                
                _logger.LogInformation($"Verification of upcoming insurance appointments has: run verification for patient id = {patient.GetId()}");
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Verification of upcoming insurance appointments has: failed to run verification for patient id = {patient.GetId()} {e}");
            }
        }
        
        _logger.LogInformation("Verification of upcoming insurance appointments has: finished");
    }

    #region private
    
    bool IsSaturday(DayOfWeek day) => day is DayOfWeek.Saturday;

    private DateTime GetBoostDate(DateTime dateTime)
    {
        var result = dateTime;
        var remainingDays = DefaultDaysBoost;

        bool IsWeekend(DayOfWeek day) => day is DayOfWeek.Saturday or DayOfWeek.Sunday;

        if (IsWeekend(dateTime.DayOfWeek))
        {
            var addDays = dateTime.DayOfWeek == DayOfWeek.Sunday ? 2 : 1;

            return dateTime.AddDays(addDays + DefaultDaysBoost).Date;
        }
        
        while (remainingDays > 0)
        {
            result = result.AddDays(1);
            if (!IsWeekend(result.DayOfWeek))
            {
                remainingDays--;
            }
        }

        return result;
    }

    #endregion
}