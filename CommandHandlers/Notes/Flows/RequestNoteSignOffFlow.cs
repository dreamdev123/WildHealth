using System;
using WildHealth.Application.Events.Notes;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Notes;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public record RequestNoteSignOffFlow(
    Note Note, 
    Employee Employee, 
    string AdditionalNote, 
    DateTime UtcNow): IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var domain = NoteDomain.Create(Note);
        
        ValidateNote(domain);

        domain.RequestSignOff(Employee, AdditionalNote, UtcNow);

        return Note.Updated() + new NoteSignOffRequestedEvent(Note, Employee.User);
    }
    
    #region private 
    
    private static void ValidateNote(NoteDomain domain)
    {
        if (domain.IsCompleted())
        {
            throw new DomainException("The note is already completed");
        }
    }

    #endregion
}