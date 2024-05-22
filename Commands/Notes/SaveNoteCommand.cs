using System;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Notes;

namespace WildHealth.Application.Commands.Notes
{
    public class SaveNoteCommand : IRequest<Note>, IValidatabe
    {
        public int? Id { get; }
        
        public string Name { get; }

        public string Title { get; }
                
        public NoteType Type { get; }
        
        public DateTime VisitDate { get; }

        public int PatientId { get; }

        public int EmployeeId { get; }

        public int? AppointmentId { get; }

        public string Content { get; }
        
        public IFormFile[]? Documents { get; set; }

        public string InternalContent { get; }
        
        public NoteLogModel[] Logs { get; }
        
        public bool IsCompleted { get; }

        public DateTime? NextCoachAppointmentDate { get; }

        public DateTime? NextProviderAppointmentDate { get; }
        
        public int? OriginalNoteId { get; }

        public SaveNoteCommand(
            int? id,
            string name,
            string title,
            NoteType type,
            DateTime visitDate,
            int patientId,
            int employeeId,
            int? appointmentId,
            string content,
            string internalContent,
            NoteLogModel[] logs,
            bool isCompleted,
            DateTime? nextCoachAppointmentDate,
            DateTime? nextProviderAppointmentDate,
            int? originalNoteId)
            {
            Id = id;
            Name = name;
            Title = title;
            Type = type;
            VisitDate = visitDate;
            PatientId = patientId;
            EmployeeId = employeeId;
            AppointmentId = appointmentId;
            Content = content;
            InternalContent = internalContent;
            Logs = logs;
            IsCompleted = isCompleted;
            NextCoachAppointmentDate = nextCoachAppointmentDate;
            NextProviderAppointmentDate = nextProviderAppointmentDate;
            OriginalNoteId = originalNoteId;
            }

        #region validation

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<SaveNoteCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0).When(x => x.Id.HasValue);
                RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
                RuleFor(x => x.Title).MaximumLength(100);
                RuleFor(x => x.PatientId).GreaterThan(0);
                RuleFor(x => x.EmployeeId).GreaterThan(0);
                RuleFor(x => x.AppointmentId).GreaterThan(0).When(x => x.AppointmentId is not null);
                RuleFor(x => x.OriginalNoteId).GreaterThan(0).When(x => x.OriginalNoteId is not null);
            }
        }

        #endregion
    }
}