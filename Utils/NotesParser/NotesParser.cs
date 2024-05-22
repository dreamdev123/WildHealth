using System;
using System.Linq;
using System.Collections.Generic;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Goals;
using WildHealth.Domain.Enums.Notes;
using Newtonsoft.Json;

namespace WildHealth.Application.Utils.NotesParser;

/// <summary>
/// <see cref="INotesParser"/>
/// </summary>
public class NotesParser : INotesParser
{
    private static readonly IDictionary<NoteType, Func<Note, NoteGoalModel[]>> Parsers =
        new Dictionary<NoteType, Func<Note, NoteGoalModel[]>>
        {
            {
                NoteType.FollowUp,
                note =>
                {
                    var oldContentModel = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

                    if (oldContentModel?.Goals is null || !oldContentModel.Goals.Any())
                    {
                        var newContentModel = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

                        return newContentModel?.Plan is null
                            ? Array.Empty<NoteGoalModel>()
                            : ExtractGoalsFromNotePlan(newContentModel.Plan);
                    }

                    // For old goals put category "Other"
                    return oldContentModel.Goals.Select(x =>
                    {
                        x.Category = GoalCategory.Other;

                        return x;
                    }).ToArray();
                }
            },
            {
                NoteType.HistoryAndPhysicalInitial,
                note =>
                {
                    var contentModel = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

                    return contentModel?.Plan is null
                        ? Array.Empty<NoteGoalModel>()
                        : ExtractGoalsFromNotePlan(contentModel.Plan);
                }
            },
            {
                NoteType.HistoryAndPhysicalFollowUp,
                note =>
                {
                    var contentModel = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

                    return contentModel?.Plan is null
                        ? Array.Empty<NoteGoalModel>()
                        : ExtractGoalsFromNotePlan(contentModel.Plan);
                }
            },
            {
                NoteType.HistoryAndPhysicalGroupVisit,
                note =>
                {
                    var contentModel = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

                    return contentModel?.Plan is null
                        ? Array.Empty<NoteGoalModel>()
                        : ExtractGoalsFromNotePlan(contentModel.Plan);
                }
            }
        };

    /// <summary>
    /// <see cref="INotesParser.ParseDuration"/>
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    public int? ParseDuration(Note note)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

            if (model?.Duration != null)
            {
                return model?.Duration?.Value;
            }

            return model?.AppointmentConfiguration?.Duration;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="INotesParser.ParseAppointmentConfiguration"/>
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public NoteAppointmentConfigurationModel? ParseAppointmentConfiguration(Note note)
    {
        try
        {
            var source = note.Type == NoteType.Initial && !note.IsOldIcc
                ? note.Content.InternalContent
                : note.Content.Content;
            
            var model = JsonConvert.DeserializeObject<NotesContentModel>(source);

            return model?.AppointmentConfiguration;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="INotesParser.ParseApoe"/>
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    public NoteApoeModel? ParseApoe(Note note)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

            return model?.Pmh?.Apoe;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="INotesParser.ParsePhysicalData"/>
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public NotePhysicalDataModel? ParsePhysicalData(Note note)
    {
        try
        {
            if (note.Type == NoteType.Initial && !note.IsOldIcc)
            {
                var model = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.InternalContent);
                    
                return new NotePhysicalDataModel
                {
                    SystolicBP = model?.SystolicBloodPressure,
                    DiastolicBP = model?.DiastolicBloodPressure
                };
            }
            else
            {
                var model = JsonConvert.DeserializeObject<NotesContentModel>(note.Content.Content);

                return model?.PhysicalData;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="INotesParser.ParseGoals"/>
    /// </summary>
    /// <param name="note"></param>
    /// <returns></returns>
    public NoteGoalModel[] ParseGoals(Note note)
    {
        if (!Parsers.ContainsKey(note.Type))
        {
            return Array.Empty<NoteGoalModel>();
        }

        try
        {
            var parser = Parsers[note.Type];

            return parser.Invoke(note);
        }
        catch
        {
            return Array.Empty<NoteGoalModel>();
        }
    }

    /// <summary>
    /// <see cref="INotesParser.ParseSupplements"/>
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public NoteSupplementModel[] ParseSupplements(NoteContent content)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<NotesContentModel>(content.Content);

            if (model is null)
            {
                return Array.Empty<NoteSupplementModel>();
            }

            return model.MedicationsAndSupplements.Supplements.ToArray();
        }
        catch
        {
            return Array.Empty<NoteSupplementModel>();
        }
    }

    /// <summary>
    /// <see cref="INotesParser.ParseMedications"/>
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public NoteMedicationModel[] ParseMedications(NoteContent content)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<NotesContentModel>(content.Content);

            if (model is null)
            {
                return Array.Empty<NoteMedicationModel>();
            }

            return model.MedicationsAndSupplements.Medications.ToArray();
        }
        catch
        {
            return Array.Empty<NoteMedicationModel>();
        }
    }

    /// <summary>
    /// <see cref="INotesParser.ParseSpecialTests"/>
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public NoteSpecialTestModel[] ParseSpecialTests(NoteContent content)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<NotesContentModel>(content.Content);

            if (model is null)
            {
                return Array.Empty<NoteSpecialTestModel>();
            }

            return model.SpecialTests.ToArray();
        }
        catch
        {
            return Array.Empty<NoteSpecialTestModel>();
        }
    }

    public string ParsePlanText(NoteContent content)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<NotesContentModel>(content.Content);

            return model is null ? string.Empty : model.Plan.PlanText;
        }
        catch
        {
            return string.Empty;
        }
    }

    public string ParseMdm(NoteContent content)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<NotesContentModel>(content.Content);

            return model is null ? string.Empty : model.Mdm.MdmText;
        }
        catch
        {
            return string.Empty;
        }
    }

    public string ParseHcPlanText(NoteContent content, NoteType noteType)
    {
        try
        {
            if (noteType == NoteType.Initial)
            {
                var internalContent = JsonConvert.DeserializeObject<NotesContentModel>(content.InternalContent);
                var hcBox = JsonConvert.DeserializeObject<string>(content.Content) ?? "";
                var internalNote = internalContent?.InternalContent ?? "";
                return (internalNote + "\n" + hcBox).Trim();
            }
            else if (noteType == NoteType.FollowUp)
            {
                var hcBox = JsonConvert.DeserializeObject<NotesContentModel>(content.Content);
                var internalNote = content.InternalContent ?? "";
                return (internalNote + "\n" + hcBox?.TodayCoachingContent ?? "").Trim();
            }
            else
                return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    #region private

    private static NoteGoalModel[] ExtractGoalsFromNotePlan(Plan plan)
    {
        foreach (var goal in plan.DietGoals ?? Array.Empty<NoteGoalModel>())
        {
            goal.Category = GoalCategory.Diet;
        }

        foreach (var goal in plan.ExerciseGoals ?? Array.Empty<NoteGoalModel>())
        {
            goal.Category = GoalCategory.Exercise;
        }

        foreach (var goal in plan.SleepGoals ?? Array.Empty<NoteGoalModel>())
        {
            goal.Category = GoalCategory.Sleep;
        }

        foreach (var goal in plan.NeuroGoals ?? Array.Empty<NoteGoalModel>())
        {
            goal.Category = GoalCategory.Neuro;
        }

        foreach (var goal in plan.LongevityGoals ?? Array.Empty<NoteGoalModel>())
        {
            goal.Category = GoalCategory.Longevity;
        }

        foreach (var goal in plan.MindfulnessGoals ?? Array.Empty<NoteGoalModel>())
        {
            goal.Category = GoalCategory.Mindfulness;
        }

        return (plan.DietGoals ?? Array.Empty<NoteGoalModel>())
            .Concat(plan.ExerciseGoals ?? Array.Empty<NoteGoalModel>())
            .Concat(plan.SleepGoals ?? Array.Empty<NoteGoalModel>())
            .Concat(plan.NeuroGoals ?? Array.Empty<NoteGoalModel>())
            .Concat(plan.LongevityGoals ?? Array.Empty<NoteGoalModel>())
            .Concat(plan.MindfulnessGoals ?? Array.Empty<NoteGoalModel>())
            .ToArray();
    }

    #endregion
}