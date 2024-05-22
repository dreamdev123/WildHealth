using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Services.Notes;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Models.Notes;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Employees;
using WildHealth.Domain.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes
{
    public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand, Note>
    {
        private readonly INoteService _noteService;
        private readonly IAppointmentsService _appointmentsService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IAuthTicket _authTicket;
        private readonly IEmployeeService _employeeService;
        private readonly MaterializeFlow _materialize;

        public UpdateNoteCommandHandler(
            INoteService noteService, 
            IAppointmentsService appointmentsService,
            IPermissionsGuard permissionsGuard,
            IAuthTicket authTicket,
            IEmployeeService employeeService, 
            MaterializeFlow materialize)
        {
            _noteService = noteService;
            _appointmentsService = appointmentsService;
            _permissionsGuard = permissionsGuard;
            _authTicket = authTicket;
            _employeeService = employeeService;
            _materialize = materialize;
        }

        public async Task<Note> Handle(UpdateNoteCommand command, CancellationToken cancellationToken)
        {
            var note = await _noteService.GetByIdAsync(command.Id);

            _permissionsGuard.AssertPermissions(note);

            AssertModified(note);

            if (command.AppointmentId.HasValue && command.AppointmentId != note.AppointmentId)
            {
                await AssertNoteWithSameAppointmentDoesNotExist(command.AppointmentId.Value);
            }

            note.Name = command.Name;
            note.Title = command.Title;
            note.VisitDate = command.VisitDate;
            note.Content.Content = command.Content;
            note.Content.InternalContent = command.InternalContent;

            // Some notes can be owned by virtual assistants
            // in this case we want to skip storing delegated action
            if (_permissionsGuard.IsAssistRole() && note.EmployeeId != _authTicket.GetEmployeeId())
            {
                var domain = NoteDomain.Create(note);

                domain.OnDelegatedAction(note.Employee);
            }

            var appointment = await GetAppointmentAsync(command.AppointmentId);

            if (command.AppointmentId != note.AppointmentId && appointment is not null)
            {
                if (appointment.PatientId != note.PatientId)
                {
                    throw new DomainException("Appointment is related to another patient");
                }
                
                note.AppointmentId = appointment.GetId();
            }

            foreach (var model in command.Logs)
            {
                var log = note.Content.Logs.FirstOrDefault(x => x.Key == model.Key);
                if (log is null)
                {
                    note.Content.Logs.Add(new NoteLog
                    {
                        Key = model.Key,
                        Value = model.Value
                    });
                }
                else
                {
                    log.Value = model.Value;
                }
            }

            var logsToDelete = note
                .Content
                .Logs
                .Where(x => command.Logs.All(t => t.Key != x.Key))
                .ToArray();

            foreach (var log in logsToDelete)
            {
                note.Content.Logs.Remove(log);
            }

            await _noteService.UpdateAsync(note);

            if (command.IsCompleted)
            {
                var completedBy = _authTicket.GetId();
                var employee = await _employeeService.GetByUserIdAsync(completedBy);
                await new CompleteNoteFlow(note, appointment, employee, DateTime.UtcNow).Materialize(_materialize);
            }

            return note;
        }

        #region private

        /// <summary>
        /// Asserts note for particular endpoint does not exist
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <exception cref="AppException"></exception>
        private async Task AssertNoteWithSameAppointmentDoesNotExist(int appointmentId)
        {
            try
            {
                var existingNote = await _noteService.GetByAppointmentIdAsync(appointmentId);

                if (existingNote is not null)
                {
                    throw new AppException(HttpStatusCode.BadRequest, "Note for this appointment already exists");
                }
            }
            catch (AppException ex) when(ex.StatusCode == HttpStatusCode.NotFound)
            {
                // ignore
            }
        }
        
        /// <summary>
        /// Returns appointment by id
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns></returns>
        private async Task<Appointment?> GetAppointmentAsync(int? appointmentId)
        {
            if (!appointmentId.HasValue)
            {
                return null;
            }

            return await _appointmentsService.GetByIdAsync(appointmentId.Value);
        }
        
        /// <summary>
        /// Asserts if note can be modified
        /// </summary>
        /// <param name="note"></param>
        /// <exception cref="AppException"></exception>
        private static void AssertModified(Note note)
        {
            var noteDomain = NoteDomain.Create(note);
            if (noteDomain.IsCompleted())
            {
                throw new AppException(HttpStatusCode.BadRequest, "Can't modify completed notes.");
            }
        }

        #endregion
    }
}