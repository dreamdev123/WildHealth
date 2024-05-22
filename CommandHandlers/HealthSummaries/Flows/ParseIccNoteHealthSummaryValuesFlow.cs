using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.CommandHandlers.HealthSummaries.Flows;

public class ParseIccNoteHealthSummaryValuesFlow
{
    private readonly Note _note;

    public ParseIccNoteHealthSummaryValuesFlow(Note note)
    {
        _note = note;
    }

    public ParseIccNoteHealthSummaryValuesFlowResult Execute()
    {
        var result = ReadNoteContentCurrentMedical(_note);

        if (result is null || result.CurrentMedicalConditions.Length <= 0)
        {
            return ParseIccNoteHealthSummaryValuesFlowResult.Empty;
        }

        var problemList = new List<HealthSummaryValue>();

        problemList.AddRange(
            result.CurrentMedicalConditions
                .Select(x => new HealthSummaryValue(
                    _note.Patient,
                    x.Key,
                    x.Value,
                    x.Name,
                    x.Tooltip))
        );

        return new ParseIccNoteHealthSummaryValuesFlowResult(problemList);
    }

    private InternalContentMedicalConditionModel? ReadNoteContentCurrentMedical(Note requestNote)
    {
        try
        {
            return JsonConvert.DeserializeObject<InternalContentMedicalConditionModel>(requestNote.Content.InternalContent);
        }
        catch
        {
            return null;
        }
    }
    
    public record ParseIccNoteHealthSummaryValuesFlowResult(List<HealthSummaryValue> ResultValues)
    {
        public static ParseIccNoteHealthSummaryValuesFlowResult Empty => new ParseIccNoteHealthSummaryValuesFlowResult(new List<HealthSummaryValue>());
    }
}