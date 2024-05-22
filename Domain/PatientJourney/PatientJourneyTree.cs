using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Extensions;
using WildHealth.Common.Models.PatientJourney;
using WildHealth.Domain.Enums.PatientJourney;
using WildHealth.Domain.Models.Exceptions;
using static WildHealth.Domain.Entities.PatientJourney.PatientJourneyTaskStatus;

namespace WildHealth.Application.Domain.PatientJourney;

public record PatientJourneyTree
{
    private readonly PaymentPlanJourneyTaskModel[] _paymentPlanTasks;
    private readonly PatientJourneyTaskReactionModel[] _patientTasks;
    private readonly List<Node> _treeNodes;

    public static PatientJourneyTree Empty { get; } = new(Array.Empty<PaymentPlanJourneyTaskModel>(),Array.Empty<PatientJourneyTaskReactionModel>());
    
    public PatientJourneyTree(PaymentPlanJourneyTaskModel[] paymentPlanTasks, PatientJourneyTaskReactionModel[] patientTasks)
    {
        _paymentPlanTasks = paymentPlanTasks;
        _patientTasks = patientTasks;
        _treeNodes = BuildTree();
    }

    private List<Node> BuildTree()  
    {
        var patientTasksDictionary = _patientTasks.ToDictionary(x => x.JourneyTaskId, x => x );
        var paymentPlanTasksLookup = _paymentPlanTasks
            .Where(x => patientTasksDictionary.ContainsKey(x.JourneyTaskId))
            .ToLookup(x => x.ParentId);
        
        var rootNodes = paymentPlanTasksLookup[null] // root nodes where ParentId is NULL
            .Select(x => new Node(x, patientTasksDictionary[x.JourneyTaskId], Children(x.Id)))
            .ToList();

        return rootNodes;

        List<Node> Children(int parentId) =>
            paymentPlanTasksLookup[parentId]
                .Select(x => new Node(x, patientTasksDictionary[x.JourneyTaskId], Children(x.Id)))
                .ToList();
    }

    public JourneyTaskCTAModel? GetTaskCTAById(int journeyTaskId)
    {
        return TraverseTree(_treeNodes);
        
        JourneyTaskCTAModel? TraverseTree(List<Node> nodes)
        {
            return nodes
                .Select(node => node.Task.Id == journeyTaskId ? node.Task.TaskCtaModel : TraverseTree(node.Children))
                .FirstOrDefault(found => found is not null);
        }
    }
    
    public bool CanUndo(int journeyTaskId)
    {
        var completed = Completed();
        return completed
            .SelectMany(x => x.RequiredTasks)
            .Concat(completed.SelectMany(y => y.OptionalTasks))
            .FirstOrDefault(t => t.Id == journeyTaskId)
            ?.CanUndo ?? false;
    }

    public List<TodoPatientJourneyTaskCategoryModel> Todos()
    {
        // root nodes where status is null
        var todoNodes = _treeNodes
            .Where(n => n.Reaction.Status is null or Active)
            .OrderBy(n => n.Task.Priority)
            .ToArray();
        
        return todoNodes.Any() ? todoNodes
            .ToLookup(x => x.Task.Group)
            .Select(x => new TodoPatientJourneyTaskCategoryModel
            {
                Category = x.Key,
                RequiredTasks = x.Where(y => y.Task.IsRequired).Select(BuildTaskModel).ToArray(),
                OptionalTasks = x.Where(y => !y.Task.IsRequired).Select(BuildTaskModel).ToArray(),
                IsLocked = x.Key != todoNodes.First().Task.Group
            }).ToList() : List.Empty<TodoPatientJourneyTaskCategoryModel>();

        TodoPatientJourneyTaskModel BuildTaskModel(Node n) => new()
        {
            Id = n.Task.JourneyTaskId,
            Title = n.Task.Title,
            Description = n.Task.Description,
            IsRequired = n.Task.IsRequired,
            Group = n.Task.Group,
            Priority = n.Task.Priority,
            TaskCTA = n.Task.TaskCtaModel
        };
    }

    public List<CompletedPatientJourneyTaskCategoryModel> Completed()
    {
        // root nodes with corresponding status
        var completed = _treeNodes
            .Where(n =>
                (n.Reaction.Status & PatientCompleted) == PatientCompleted ||
                (n.Reaction.Status & AutoCompleted) == AutoCompleted ||
                n.Reaction.Status == Dismissed)
            .ToList();
        
        return completed
            .ToLookup(x => x.Task.Group)
            .Select(x => new CompletedPatientJourneyTaskCategoryModel
            {
                Category = x.Key,
                RequiredTasks = x.Where(y => y.Task.IsRequired).Select(BuildTaskModel).ToArray(),
                OptionalTasks = x.Where(y => !y.Task.IsRequired).Select(BuildTaskModel).ToArray(),
            }).ToList();
        
        CompletedPatientJourneyTaskModel BuildTaskModel(Node n) => new()
        {
            Id = n.Task.JourneyTaskId,
            Title = n.Task.Title,
            Group = n.Task.Group,
            CompletedBy = CalcCompletedBy(n),
            IsDismissed = n.Reaction.Status == Dismissed,
            CanUndo = (n.Reaction.Status & AutoCompleted) != AutoCompleted,
            Description = n.Task.Description,
            Priority = n.Task.Priority,
            TaskCTA = n.Task.TaskCtaModel
        };
        
        string CalcCompletedBy(Node node)
        {
            if (node.Reaction.Status == Dismissed)
                return "patient";
            if ((node.Reaction.Status & PatientCompleted) == PatientCompleted)
                return "patient";
            if ((node.Reaction.Status & AutoCompleted) == AutoCompleted)
                return "auto-completed";
            
            return "unknown";
        }
    }
    
    public List<PatientJourneyRewardModel> Rewards()
    {
        // all children of completed nodes are Rewards
        return _treeNodes
            .Where(n =>
                (n.Reaction.Status & PatientCompleted) == PatientCompleted ||
                (n.Reaction.Status & AutoCompleted) == AutoCompleted)
            .SelectMany(n => n.Children)
            .Select(n => new PatientJourneyRewardModel
            {
                Id = n.Task.Id,
                Title = n.Task.Title,
                Description = n.Task.Description,
                Priority = n.Task.Priority,
                TaskCTA = n.Task.TaskCtaModel
            }).ToList();
    }

    public bool HasReward(int journeyTaskId) => _treeNodes
        .Any(x => x.Task.Id == journeyTaskId && x.Children.Any()); 

    public int[] GetTasksQualifiedForAutoCompletion(AutomaticCompletionPrerequisite prerequisite)
    {
        var qualifiedForCompletion = _paymentPlanTasks
            .Where(t => t.AutomaticCompletionPrerequisite == prerequisite)
            .Select(t => t.JourneyTaskId)
            .ToArray();
        
        var todos = Todos();

        return todos
            .SelectMany(x => x.RequiredTasks)
            .Concat(todos.SelectMany(x => x.OptionalTasks))
            .Where(x => qualifiedForCompletion.Contains(x.Id))
            .ToLookup(x => x.Title) // we have multiple tasks with the same title (e.g. 'Book a Physician Visit') 
            .Select(grouping => grouping.OrderBy(y => y.Id).First()) // take first task in order 
            .Select(x => x.Id)
            .ToArray();
    }

    public void ThrowIfEmpty()
    {
        if (this == Empty)
            throw new EntityNotFoundException("Patient Journey feature is not available for the current plan");
    }
    
    /// <summary>
    /// Patient Journey Tree Node
    /// </summary>
    /// <param name="Task">Journey Task</param>
    /// <param name="Reaction">Current Journey Task status</param>
    /// <param name="Children">Represents Rewards</param>
    private record Node(PaymentPlanJourneyTaskModel Task, PatientJourneyTaskReactionModel Reaction, List<Node> Children);
}