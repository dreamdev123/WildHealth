using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Appointments.Flows;
using WildHealth.Application.Events.Notes;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Commands.Timezones;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.NotesParser;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Models.Notes;
using WildHealth.Application.Services.Appointments;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Enums.Payments;
using MediatR;

namespace WildHealth.Application.EventHandlers.Notes;

public class CreatePastAppointmentOnNoteCompletedEvent : INotificationHandler<NoteCompletedEvent>
{
    private readonly IAppointmentsService _appointmentsService;
    private readonly IFlowMaterialization _materialization;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmployeeService _employeeService;
    private readonly INotesParser _notesParser;
    private readonly IMediator _mediator;

    public CreatePastAppointmentOnNoteCompletedEvent(
        IAppointmentsService appointmentsService,
        IFlowMaterialization materialization, 
        IDateTimeProvider dateTimeProvider, 
        IEmployeeService employeeService, 
        INotesParser notesParser, 
        IMediator mediator)
    {
        _appointmentsService = appointmentsService;
        _materialization = materialization;
        _dateTimeProvider = dateTimeProvider;
        _employeeService = employeeService;
        _notesParser = notesParser;
        _mediator = mediator;
    }

    public async Task Handle(NoteCompletedEvent @event, CancellationToken cancellationToken)
    {
        var note = @event.Note;
        var domain = NoteDomain.Create(note);

        if (!domain.HasAppointment() && domain.IsAppointmentRelated() && !domain.IsAmended())
        {
            var employee = await _employeeService.GetByIdAsync(note.CompletedById ?? 0);
            var timeZone = await GetTimeZoneAsync(employee);
            var duration = _notesParser.ParseDuration(note);
            var configurationModel = _notesParser.ParseAppointmentConfiguration(note);
            var (type, configuration) = configurationModel is not null
                ? await GetAppointmentTypeConfiguration(configurationModel)
                : (null, null);
            
            var flow = new CreatePastAppointmentFlow(
                Patient: note.Patient,
                Employee: note.Employee,
                Note: note,
                Duration: duration,
                TimeZone: timeZone,
                Type: type,
                Configuration: configuration,
                UtcNow: _dateTimeProvider.UtcNow()
            );

            await flow.Materialize(_materialization.Materialize);
        }
    }
    
    #region private

    private Task<TimeZoneInfo> GetTimeZoneAsync(Employee employee)
    {
        var command = GetCurrentTimezoneCommand.ForEmployee(employee.GetId(), employee.User.PracticeId);
        
        return _mediator.Send(command);
    }
    
    private async Task<(AppointmentType?, AppointmentTypeConfiguration?)> GetAppointmentTypeConfiguration(NoteAppointmentConfigurationModel model)
    {
        var types = await _appointmentsService.GetAllTypesAsync((int)PlanPlatform.WildHealth);

        var type = types.FirstOrDefault(x => x.Configurations.Any(t => t.Id == model.ConfigurationId));

        var configuration = type?.Configurations.FirstOrDefault(x => x.Id == model.ConfigurationId);

        return (type, configuration);
    }
    
    #endregion
}