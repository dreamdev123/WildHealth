using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Utils.NotesParser;
using WildHealth.Common.Models.Notes;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Notes;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Notes
{
    /// <summary>
    /// <see cref="INoteService"/>
    /// </summary>
    public class NoteService : INoteService
    {
        private static readonly NoteType[] NotTypesWithGoals =
        {
            NoteType.FollowUp,
            NoteType.HistoryAndPhysicalInitial,
            NoteType.HistoryAndPhysicalFollowUp,
            NoteType.HistoryAndPhysicalGroupVisit
        };
        
        private readonly IGeneralRepository<Note> _notesRepository;
        private readonly INotesParser _notesParser;

        public NoteService(
            IGeneralRepository<Note> notesRepository,
            INotesParser notesParser)
        {
            _notesRepository = notesRepository;
            _notesParser = notesParser;
        }

        /// <summary>
        /// <see cref="INoteService.GetContentByNoteIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<NoteContent> GetContentByNoteIdAsync(int id)
        {
            var noteContent = await _notesRepository
                .All()
                .ById(id)
                .SelectContent()
                .FirstAsync();
            
            return noteContent;
        }

        /// <summary>
        /// <see cref="INoteService.CreateAsync"/>
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task<Note> CreateAsync(Note note)
        {
            await _notesRepository.AddAsync(note);

            await _notesRepository.SaveAsync();

            return note;
        }
        
        /// <summary>
        /// <see cref="INoteService.UpdateAsync(Note)"/>
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task<Note> UpdateAsync(Note note)
        {
            _notesRepository.EditRelated(note);

            await _notesRepository.SaveAsync();

            return note;
        }

        public async Task<NoteContent> UpdateAsync(NoteContent noteContent)
        {
            _notesRepository.EditRelated(noteContent);

            await _notesRepository.SaveAsync();

            return noteContent;
        }

        /// <summary>
        /// <see cref="INoteService.GetAllNotesIdsAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<int?[]> GetAllNotesIdsAsync(int from, int to)
        {
            var notesIds = await _notesRepository
                .All()
                .Where(x => x.Id >= from && x.Id < to && (x.Type == NoteType.Blank || x.Type == NoteType.Internal))
                .Select(x => x.Id)
                .ToArrayAsync();
            
            return notesIds;
        }

        /// <summary>
        /// <see cref="INoteService.GetSimpleByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Note> GetSimpleByIdAsync(int id)
        {
            var notes = await _notesRepository
                .All()
                .ById(id)
                .IncludeUsers()
                .FirstOrDefaultAsync();

            if (notes is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Visit notes do not exist.", exceptionParam);
            }

            return notes;
        }

        /// <summary>
        /// <see cref="INoteService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Note> GetByIdAsync(int id)
        {
            var notes = await _notesRepository
                .All()
                .ById(id)
                .IncludeUsers()
                .IncludeContent()
                .IncludeOriginal()
                .IncludeAmendedNotes()
                .IncludeAppointment()
                .IncludeActors<Note>()
                .IncludePdf()
                .IncludeClaims()
                .FirstOrDefaultAsync();

            if (notes is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Visit notes do not exist.", exceptionParam);
            }

            return notes;
        }

        /// <summary>
        /// <see cref="INoteService.GetByAppointmentIdAsync"/>
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Note> GetByAppointmentIdAsync(int appointmentId)
        {
            var note = await _notesRepository
                .All()
                .ByAppointmentId(appointmentId)
                .FirstOrDefaultAsync();

            if (note is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(appointmentId), appointmentId);
                throw new AppException(HttpStatusCode.NotFound, "Visit note for appointments does not exist.", exceptionParam);
            }

            return note;
        }

        /// <summary>
        /// <see cref="INoteService.GetPatientNotesAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="onlyCompleted"></param>
        /// <param name="type"></param>
        /// <param name="includeInternalNotes"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Note>> GetPatientNotesAsync(int patientId, bool onlyCompleted, NoteType? type = null, bool includeInternalNotes = true)
        {
            var visitNotesQuery = _notesRepository
                .All()
                .RelatedToPatient(patientId)
                .ByType(type)
                .IncludeAppointment()
                .IncludeInternalNotes(includeInternalNotes)
                .IncludeUsers()
                .IncludeOriginal()
                .IncludeAppointmentOptions()
                .IncludeAmendedNotes()
                .OnlyOriginal();

            if (onlyCompleted)
            {
                visitNotesQuery = visitNotesQuery
                    .OnlyCompleted()
                    .NotDeleted();
            }

            return await visitNotesQuery.ToArrayAsync();
        }

        /// <summary>
        /// <see cref="INoteService.GenerateFileName"/>
        /// </summary>
        /// <param name="noteContentId"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public string GenerateFileName(int noteContentId, string fileName, string fileSize)
        {
            return $"Notes/{noteContentId}/{Guid.NewGuid().ToString().Substring(0,8)}/{fileSize}/{fileName}";
        }

        /// <summary>
        /// <see cref="INoteService.GetPatientDraftNotesAsync"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="sortingSource"></param>
        /// <param name="sortingDirection"></param>
        /// <returns></returns>
        public async Task<(IEnumerable<Note>, int totalCount)> GetPatientDraftNotesAsync(int employeeId, int? skip, int? take, string? sortingSource, string? sortingDirection)
        {
            var draftNotesQuery = _notesRepository
                .All()
                .RelatedToEmployee(employeeId)
                .OnlyIncomplete()
                .IncludeOriginal()
                .IncludeAppointment()
                .IncludeInternalNotes(true)
                .IncludeUsers()
                .IncludeAppointmentOptions()
                .Sort(sortingSource,sortingDirection);

           var totalCount = await draftNotesQuery.CountAsync();
           var notes = await draftNotesQuery.Pagination(skip, take).ToArrayAsync();
            return (notes, totalCount);
        }


        /// <summary>
        /// <see cref="INoteService.GetContentAsync"/>
        /// </summary>
        /// <param name="visitNotesId"></param>
        /// <param name="patientId"></param>
        /// <param name="includeInternalNotes"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<NoteContent> GetContentAsync(int visitNotesId, int patientId, bool includeInternalNotes = true)
        {
            var result = await _notesRepository
                .All()
                .FindNotes(visitNotesId, patientId)
                .IncludeContent()
                .IncludeAttachments()
                .IncludeAppointment()
                .IncludeInternalNotes(includeInternalNotes)
                .SelectContent()
                .FirstOrDefaultAsync();

            if (result is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(visitNotesId), visitNotesId);
                throw new AppException(HttpStatusCode.NotFound, "Visit notes do not exist", exceptionParam);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<NoteGoalModel[]> GetPatientGoalsAsync(int patientId)
        {
            var lastCompletedNote = await _notesRepository
                .All()
                .RelatedToPatient(patientId)
                .ByTypes(NotTypesWithGoals)
                .OnlyCompleted()
                .IncludeContent()
                .IncludeUsers()
                .OrderByDescending(x => x.CompletedAt)
                .IncludeAmendedNotes()
                .OnlyOriginal()
                .FirstOrDefaultAsync();

            if (lastCompletedNote is null)
            {
                return Array.Empty<NoteGoalModel>();
            }

            return _notesParser.ParseGoals(lastCompletedNote);
        }

        /// <summary>
        /// <see cref="INoteService.GetAssignedForSignOff"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="noteTypes"></param>
        /// <param name="sortingSource"></param>
        /// <param name="sortingDirection"></param>
        /// <returns></returns>
        public async Task<(Note[], int)> GetAssignedForSignOff(int employeeId,
            int? skip = 0,
            int? take = 50,
            NoteType[]? noteTypes = null,
            string? sortingSource = null,
            string? sortingDirection = null)
        {
            var queryData = _notesRepository
                .All()
                .IncludeUsers()
                .IncludeAppointment()
                .IncludeOriginal()
                .OnlyIncomplete()
                .ByTypes(noteTypes)
                .AssignedForSignOffToEmployee(employeeId)
                .Sort(sortingSource, sortingDirection);
                
            var totalCount = await queryData.CountAsync();
            var notes = await queryData.Pagination(skip, take).ToArrayAsync();
            return (notes, totalCount);
        }

        /// <summary>
        /// <see cref="INoteService.GetNotesUnderReview"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="noteTypes"></param>
        /// <param name="sortingSource"></param>
        /// <param name="sortingDirection"></param>
        /// <returns></returns>
        public async Task<(Note[], int)> GetNotesUnderReview(int employeeId,
            int? skip = 0,
            int? take = 50,
            NoteType[]? noteTypes = null,
            string? sortingSource = null,
            string? sortingDirection = null)
        {
            var queryData = _notesRepository
                .All()
                .OnlyIncomplete()
                .ByTypes(noteTypes)
                .RelatedToEmployee(employeeId)
                .IncludeUsers()
                .IncludeOriginal()
                .AssignedForSignOff()
                .Sort(sortingSource,sortingDirection);
            
            var totalCount = await queryData.CountAsync();
            var notes = await queryData.Pagination(skip, take).ToArrayAsync();
            return (notes, totalCount);
        }

        /// <summary>
        /// <see cref="INoteService.DeleteAsync"/>
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task DeleteAsync(Note note)
        {
            _notesRepository.Delete(note);

            await _notesRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="INoteService.GetNextVersionNumber"/>
        /// </summary>
        /// <param name="originalNoteId"></param>
        /// <returns></returns>
        public async Task<int> GetNextVersionNumber(int? originalNoteId)
        {
            if (!originalNoteId.HasValue) return 0;
            var latestVersion = await _notesRepository
                    .All()
                    .Where(x => x.OriginalNoteId == originalNoteId)
                    .MaxAsync(x => (int?)x.Version) ?? 0;

            return latestVersion + 1;
        }

        /// <summary>
        /// <see cref="INoteService.NotesUnderReviewExistAsync"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public async Task<bool> NotesUnderReviewExistAsync(int employeeId)
        {
            return await _notesRepository
                .All()
                .OnlyIncomplete()
                .AssignedForSignOffToEmployee(employeeId)
                .AnyAsync();
        }
    }
}
