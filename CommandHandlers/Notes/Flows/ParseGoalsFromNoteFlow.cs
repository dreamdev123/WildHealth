using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Goals;
using WildHealth.Domain.Entities.Patients;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.HealthPlans;
using WildHealth.IntegrationEvents.HealthPlans.Payloads;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public class ParseGoalsFromNoteFlow: IMaterialisableFlow
{
    private readonly Patient _patient;
    private readonly Goal[] _currentGoals;
    private readonly NoteGoalModel[] _noteGoals;
    private readonly DateTime _utcNow;

    public ParseGoalsFromNoteFlow(
        Patient patient, 
        Goal[] currentGoals, 
        NoteGoalModel[] noteGoals,
        DateTime utcNow)
    {
        _patient = patient;
        _currentGoals = currentGoals;
        _noteGoals = noteGoals;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        if (!_noteGoals.Any())
        {
            return MaterialisableFlowResult.Empty;
        }

        var createdGoals = GetCreatedGoals(_currentGoals, _noteGoals, _patient);
        var updatedGoals = UpdateExistingGoals(_currentGoals, _noteGoals);
        var deletedGoals = GetDeletedGoals(_currentGoals, _noteGoals);

        return
            deletedGoals.Select(x => x.Deleted()).ToFlowResult()
            + updatedGoals.Select(x => x.Updated()).ToFlowResult()
            + createdGoals.Select(x => x.Added()).ToFlowResult()
            + GetIntegrationEvent(createdGoals.Concat(updatedGoals).ToArray());
    }
    
    #region private

    private HealthPlanIntegrationEvent GetIntegrationEvent(Goal[] updatedGoals)
    {
        var payload = new HealthPlanUpdatedPayload(
            topGoals: updatedGoals.Count(x => x.IsTopGoal),
            pastGoals: updatedGoals.Count(x => x.IsCompleted),
            allGoals: updatedGoals.Length);

        var patientMetadata = new PatientMetadataModel(_patient.GetId(), _patient.User.UserId());
        return new HealthPlanIntegrationEvent(payload, patientMetadata, _utcNow);
    }
    private Goal[] GetCreatedGoals(Goal[] currentGoals, NoteGoalModel[] noteGoals, Patient patient)
    {
        var currentGoalIds = currentGoals.Select(x => (long)x.GetId()).ToArray();
        
        return noteGoals
            .Where(x => !currentGoalIds.Contains(x.Id))
            .Select(x => new Goal(patient)
            {
                Name = x.Name,
                Category = x.Category,
                CompletionDate = x.CompletionDate,
                IsCompleted = x.IsCompleted,
                IsTopGoal = x.IsTopGoal,
                Interventions = x.Interventions.Select(k => new Intervention
                {
                    Name = k.Name
                }).ToList()
            }).ToArray();
    }
    
    private Goal[] GetDeletedGoals(Goal[] currentGoals, NoteGoalModel[] noteGoals)
    {
        var noteGoalIds = noteGoals.Select(x => x.Id).ToArray();
        
        return currentGoals
            .Where(x => !noteGoalIds.Contains(x.GetId()))
            .ToArray();
    }

    private Goal[] UpdateExistingGoals(Goal[] currentGoals, NoteGoalModel[] noteGoals)
    {
        var noteGoalIds = noteGoals.Select(x => x.Id).ToArray();

        return currentGoals
            .Where(x => noteGoalIds.Contains(x.GetId()))
            .Select(x =>
            {
                var noteGoal = noteGoals.First(k => k.Id == x.GetId());

                x.Name = noteGoal.Name;
                x.Category = noteGoal.Category;
                x.CompletionDate = noteGoal.CompletionDate;
                x.IsCompleted = noteGoal.IsCompleted;
                x.IsTopGoal = noteGoal.IsTopGoal;

                x.Interventions = noteGoal.Interventions.Select(k => new Intervention
                {
                    Name = k.Name
                }).ToList();
                return x;
            }).ToArray();
    }
    
    #endregion
}