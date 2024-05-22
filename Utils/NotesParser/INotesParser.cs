using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Notes;

namespace WildHealth.Application.Utils.NotesParser;

/// <summary>
/// Provides methods for parsing notes
/// </summary>
public interface INotesParser
{
    /// <summary>
    /// Parses and returns note duration
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    int? ParseDuration(Note note);

    /// <summary>
    /// Parses and returns appointment configuration
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    NoteAppointmentConfigurationModel? ParseAppointmentConfiguration(Note note);
    
    /// <summary>
    /// Parses and returns apoe from note
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    NoteApoeModel? ParseApoe(Note note);

    /// <summary>
    /// Parses and returns physical data from note
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    NotePhysicalDataModel? ParsePhysicalData(Note note);
    
    /// <summary>
    /// Parses and returns goals from note
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    NoteGoalModel[] ParseGoals(Note note);
    
    /// <summary>
    /// Parses and returns supplements
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    NoteSupplementModel[] ParseSupplements(NoteContent content);
    
    /// <summary>
    /// Parses and returns medications
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    NoteMedicationModel[] ParseMedications(NoteContent content);
    
    /// <summary>
    /// Parses and returns special tests
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    NoteSpecialTestModel[] ParseSpecialTests(NoteContent content);

    string ParsePlanText(NoteContent content);
    string ParseMdm(NoteContent content);
    string ParseHcPlanText(NoteContent content, NoteType noteType);
}