using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.Commands.Notes
{
    public class UpdateNoteCommand : IRequest<Note>, IValidatabe
    {
        public int Id { get; }
        
        public string Name { get; }
        
        public string Title { get; }
        
        public DateTime VisitDate { get; }
        
        public int? AppointmentId { get; }

        public string Content { get; }

        public string InternalContent { get; }
        
        public NoteLogModel[] Logs { get; }

        public bool IsCompleted { get; }

        public UpdateNoteCommand(
            int id,
            string name,
            string title,
            DateTime visitDate,
            int? appointmentId,
            string content,
            string internalContent,
            NoteLogModel[] logs,
            bool isCompleted)
        {
            Id = id;
            Name = name;
            Title = title;
            VisitDate = visitDate;
            AppointmentId = appointmentId;
            Content = content;
            InternalContent = internalContent;
            Logs = logs;
            IsCompleted = isCompleted;
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

        private class Validator : AbstractValidator<UpdateNoteCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
                RuleFor(x => x.Title).MaximumLength(100);
                RuleFor(x => x.AppointmentId)
                    .GreaterThan(0)
                    .When(x => x.AppointmentId.HasValue);
            }
        }

        #endregion
    }
}