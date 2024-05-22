using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Notes;

namespace WildHealth.Application.Services.Notes
{
    /// <summary>
    /// Represents methods for working with notes
    /// </summary>
    public interface INoteService
    {
        /// <summary>
        /// Returns all note ids
        /// </summary>
        /// <returns></returns>
        Task<int?[]> GetAllNotesIdsAsync(int from, int to);
        
        /// <summary>
        /// Returns notes by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Note> GetSimpleByIdAsync(int id);
        
        /// <summary>
        /// Returns notes by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Note> GetByIdAsync(int id);
        
        /// <summary>
        /// Returns notes by appointment id
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns></returns>
        Task<Note> GetByAppointmentIdAsync(int appointmentId);

        /// <summary>
        /// Returns all patient notes
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="onlyCompleted"></param>
        /// <param name="type"></param>
        /// <param name="includeInternalNotes"></param>
        /// <returns></returns>
        Task<IEnumerable<Note>> GetPatientNotesAsync(int patientId, bool onlyCompleted, NoteType? type = null, bool includeInternalNotes = true);

        /// <summary>
        /// Return file name for note attachment
        /// </summary>
        /// <param name="noteContentId"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public string GenerateFileName(int noteContentId, string fileName, string fileSize);

        /// <summary>
        /// Returns all draft patient notes
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="sortingSource"></param>
        /// <param name="sortingDirection"></param>
        /// <returns></returns>
        Task<(IEnumerable<Note>, int totalCount)> GetPatientDraftNotesAsync(int employeeId,
            int? skip,
            int? take,
            string? sortingSource,
            string? sortingDirection);

        /// <summary>
        /// Return note content by note id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<NoteContent> GetContentByNoteIdAsync(int id);

        /// <summary>
        /// Creates notes
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        Task<Note> CreateAsync(Note note);

        /// <summary>
        /// Updates notes
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        Task<Note> UpdateAsync(Note note);

        /// <summary>
        /// Updates note content
        /// </summary>
        /// <param name="noteContent"></param>
        /// <returns></returns>
        Task<NoteContent> UpdateAsync(NoteContent noteContent);

        /// <summary>
        /// Returns notes content
        /// </summary>
        /// <param name="visitNotesId"></param>
        /// <param name="patientId"></param>
        /// <param name="includeInternalNotes"></param>
        /// <returns></returns>
        Task<NoteContent> GetContentAsync(int visitNotesId, int patientId, bool includeInternalNotes = true);

        /// <summary>
        /// Returns patient goals from follow up notes
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<NoteGoalModel[]> GetPatientGoalsAsync(int patientId);

        /// <summary>
        /// Returns paginated notes assigned to the employee for sign-off
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="noteTypes"></param>
        /// <param name="sortingSource"></param>
        /// <param name="sortingDirection"></param>
        /// <returns></returns>
        Task<(Note[], int)> GetAssignedForSignOff(int employeeId,
            int? skip = 0,
            int? take = 50,
            NoteType[]? noteTypes = null,
            string? sortingSource = null,
            string? sortingDirection = null);

        /// <summary>
        /// Returns paginated notes of the employee that are assigned for review
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="noteTypes"></param>
        /// <param name="sortingSource"></param>
        /// <param name="sortingDirection"></param>
        /// <returns></returns>
        Task<(Note[], int)> GetNotesUnderReview(int employeeId,
            int? skip = 0,
            int? take = 50,
            NoteType[]? noteTypes = null,
            string? sortingSource = null,
            string? sortingDirection = null);

        /// <summary>
        /// Deletes note
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        Task DeleteAsync(Note note);

        /// <summary>
        /// Get next version number for the amended note
        /// </summary>
        /// <param name="originalNoteId"></param>
        /// <returns></returns>
        Task<int> GetNextVersionNumber(int? originalNoteId);

        /// <summary>
        /// Returns true if there are any notes for review for the specific employee
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        Task<bool> NotesUnderReviewExistAsync(int employeeId);
    }
}
