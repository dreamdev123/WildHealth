using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WildHealth.Common.Models.Alergies;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.CommandHandlers.HealthSummaries.Flows;

public class ParseNoteToHealthSummaryFlow
{
    private const string NotesKeyPrefix = "NOTES";
    private const string DiagnosisKey = "NOTES_DIAGNOSIS";
    private const string HpiKey = "HPI";

    private static readonly string[] PhmSection =
    {
        "FAMILY_HISTORY",
        "PROBLEMS_LIST",
        "SOCIAL_HISTORY",
        "LIFESTYLE_HISTORY",
        "SCREENING_HEALTH_MAINTENANCE",
        "SURGICAL_HISTORY"
    };

    private readonly HealthSummaryMap[] _healthSummaryMaps;
    private readonly Note _note;

    public ParseNoteToHealthSummaryFlow(
        HealthSummaryMap[] healthSummaryMaps,
        Note note)
    {
        _healthSummaryMaps = healthSummaryMaps;
        _note = note;
    }

    public ParseNoteToHealthSummaryFlowResult Execute()
    {
        var patient = _note.Patient;

        var keysForDelete = new List<string>();

        var notesKeys = _healthSummaryMaps
            .SelectMany(x => x.Items)
            .Where(x => x.Key.Contains(NotesKeyPrefix))
            .Select(x => x.Key)
            .ToArray();

        var healthSummaries = new List<HealthSummaryValue>();

        var noteContent = ReadNoteContents(_note);
        
        if (noteContent == null)
        {
            return ParseNoteToHealthSummaryFlowResult.Empty;
        }

        if (notesKeys.Contains(DiagnosisKey))
        {
            healthSummaries.AddRange(ParseDiagnosis(_note.Patient, noteContent));
        }

        var hpi = string.IsNullOrEmpty(noteContent.Hpi)
            ? noteContent.Subjective
            : noteContent.Hpi;

        if (!string.IsNullOrEmpty(hpi))
        {
            keysForDelete.Add(HpiKey);

            healthSummaries.Add(new HealthSummaryValue(
                patient,
                HpiKey,
                hpi));
        }

        if (noteContent.Pmh != null)
        {
            keysForDelete.AddRange(
                _healthSummaryMaps
                    .Where(x => PhmSection.Contains(x.Key))
                    .SelectMany(x => x.Items.Select(t => t.Key))
                    .ToArray());

            keysForDelete.AddRange(PhmSection);

            healthSummaries.AddRange(
                noteContent.Pmh.FamHx
                    .Select(x => new HealthSummaryValue(
                        patient,
                        x.Key,
                        x.Value,
                        x.Name,
                        x.Tooltip))
            );

            healthSummaries.AddRange(
                noteContent.Pmh.CurrentMedicalConditions
                    .Select(x => new HealthSummaryValue(
                        patient,
                        x.Key,
                        x.Value,
                        x.Name,
                        x.Tooltip))
            );

            healthSummaries.AddRange(
                noteContent.Pmh.SocialHx
                    .Select(x => new HealthSummaryValue(
                        patient,
                        x.Key,
                        x.Value,
                        x.Name,
                        x.Tooltip))
            );

            healthSummaries.AddRange(
                noteContent.Pmh.LifestyleHx
                    .Select(x => new HealthSummaryValue(
                        patient,
                        x.Key,
                        x.Value,
                        x.Name,
                        x.Tooltip))
            );

            healthSummaries.AddRange(
                noteContent.Pmh.SurgicalHx
                    .Select(x => new HealthSummaryValue(
                        patient,
                        x.Key,
                        x.Value,
                        x.Name,
                        x.Tooltip))
            );

            healthSummaries.AddRange(
                noteContent.Pmh.ScreeningHealthMaintenance
                    .Select(x => new HealthSummaryValue(
                        patient,
                        x.Key,
                        x.Value,
                        x.Name,
                        x.Tooltip))
            );
        }

        var allergies = new List<CreatePatientAlergyModel>();
        if (noteContent.Pmh?.Allergies != null)
        {
            foreach (var healthSummaryValueModel in noteContent.Pmh.Allergies)
            {
                allergies.Add(new CreatePatientAlergyModel
                {
                    Name = healthSummaryValueModel.Name,
                    Reaction = healthSummaryValueModel.Value
                });
            }
        }

        if (healthSummaries.Any() || allergies.Count > 0)
        {
            return new ParseNoteToHealthSummaryFlowResult(healthSummaries, keysForDelete.ToArray(), allergies.ToArray());
        }
        
        return ParseNoteToHealthSummaryFlowResult.Empty;
    }

    private NotesContentModel? ReadNoteContents(Note requestNote)
    {
        try
        {
            return JsonConvert.DeserializeObject<NotesContentModel>(requestNote.Content.Content);
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<HealthSummaryValue> ParseDiagnosis(Patient patient, NotesContentModel noteContent)
    {
        if (noteContent.Diagnosis == null)
            //Fine.
            //The diagnosis is not required.
            return Enumerable.Empty<HealthSummaryValue>();
        
        return noteContent.Diagnosis
            .Select(x => new HealthSummaryValue(
                patient,
                $"{DiagnosisKey}-{x.Id}",
                x.Code,
                x.Description,
                x.AdditionalInformation));
    }
}

public record ParseNoteToHealthSummaryFlowResult(
    List<HealthSummaryValue> ResultValues,
    string[] KeysForRemove,
    CreatePatientAlergyModel[] Allergies)
{
    public static ParseNoteToHealthSummaryFlowResult Empty => new ParseNoteToHealthSummaryFlowResult(new List<HealthSummaryValue>(), Array.Empty<string>(), Array.Empty<CreatePatientAlergyModel>());
}