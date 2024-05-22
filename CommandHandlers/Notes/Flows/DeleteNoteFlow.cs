using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Notes;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Notes;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public record DeleteNoteFlow(Note Note, Employee Employee, NoteDeletionReason Reason, DateTime UtcNow): IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var domain = NoteDomain.Create(Note);

        if (!domain.IsCompleted())
        {
            return Note.Deleted();
        }

        AssertNoteCanBeDeleted(domain);

        domain.Delete(Employee, Reason, UtcNow);
        
        return Note.Updated();
    }
    
    #region private

    /// <summary>
    /// Asserts if note can be modified
    /// </summary>
    /// <param name="domain"></param>
    /// <exception cref="AppException"></exception>
    private void AssertNoteCanBeDeleted(NoteDomain domain)
    {
        if (domain.IsDeleted())
        {
            throw new DomainException("Note already deleted");
        }
        
        if (domain.IsCompleted())
        {
            if (Note.CompletedById != Employee.GetId())
            {
                throw new DomainException("Completed notes can only be deleted by the user that signed them");
            }

            if (Note.AmendedNotes.Any() || Note.OriginalNoteId.HasValue)
            {
                throw new DomainException("Amended notes cannot be deleted");
            }
        }
        else
        {
            if (Note.AssignedToId.HasValue)
            {
                throw new DomainException("Notes cannot be deleted while under review by another user");
            }
        }
    }

    #endregion
}