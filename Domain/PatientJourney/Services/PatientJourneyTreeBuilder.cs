using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.PatientJourney;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.PatientJourney;
using WildHealth.Domain.Models.Exceptions;
using WildHealth.Settings;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.DistributedCache.Services;
using JourneyTaskCTA = WildHealth.Domain.Entities.PatientJourney.JourneyTaskCTA;

namespace WildHealth.Application.Domain.PatientJourney.Services;

public interface IPatientJourneyTreeBuilder
{
    Task<PatientJourneyTree> Build(int patientId, int paymentPlanId, int practiceId);
}

public class PatientJourneyTreeBuilder : IPatientJourneyTreeBuilder
{
    private readonly IGeneralRepository<JourneyTask> _journeyTasksRepository;
    private readonly IGeneralRepository<PatientJourneyTask> _patientJourneyTasksRepository;
    private readonly IGeneralRepository<PaymentPlanJourneyTask> _paymentPlanRepository;
    private readonly IWildHealthSpecificCacheService<PatientJourneyTreeBuilder, ICollection<PaymentPlanJourneyTaskModel>> _cache;
    private readonly AppOptions _options;
    private readonly ISettingsManager _settingsManager;
    private readonly IPatientsService _patientsService;

    private static readonly string[] SettingNames = {SettingsNames.General.ApplicationBaseUrl};

    private const int LabsJourneyTaskId = 3;
    
    public PatientJourneyTreeBuilder(
        IGeneralRepository<JourneyTask> journeyTasksRepository, 
        IGeneralRepository<PatientJourneyTask> patientJourneyTasksRepository, 
        IGeneralRepository<PaymentPlanJourneyTask> paymentPlanRepository, 
        IWildHealthSpecificCacheService<PatientJourneyTreeBuilder, ICollection<PaymentPlanJourneyTaskModel>> cache,
        IOptions<AppOptions> options, 
        ISettingsManager settingsManager, 
        IPatientsService patientsService)
    {
        _journeyTasksRepository = journeyTasksRepository;
        _patientJourneyTasksRepository = patientJourneyTasksRepository;
        _paymentPlanRepository = paymentPlanRepository;
        _cache = cache;
        _settingsManager = settingsManager;
        _patientsService = patientsService;
        _options = options.Value;
    }

    public async Task<PatientJourneyTree> Build(int patientId, int paymentPlanId, int practiceId)
    {
        var paymentPlanJourneyTasks = await _cache.GetAsync(
            key: $"PatientJourneyTreeCache_{paymentPlanId}",
            getter: async () => await GetJourneyTreeData(paymentPlanId, practiceId)
        );
        
        var journeyTaskIds = paymentPlanJourneyTasks.Select(x => x.JourneyTaskId).ToArray();

        if (!journeyTaskIds.Any())
            return PatientJourneyTree.Empty;

        if (!await _patientsService.HasLabs(patientId))
            journeyTaskIds = journeyTaskIds.Where(id => id != LabsJourneyTaskId).ToArray();
        
        var patientJourneyTasks = _patientJourneyTasksRepository.All().Where(x => x.PatientId == patientId);
       
        var patientTasks = await
            (from task in _journeyTasksRepository.All().ByIds(journeyTaskIds)
            join pt in patientJourneyTasks on task.Id equals pt.JourneyTaskId into leftJoin
            from patientTask in leftJoin.DefaultIfEmpty()
            select new PatientJourneyTaskReactionModel
            {
                JourneyTaskId = task.Id!.Value,
                Status = patientTask.Status,
                PatientTaskId = patientTask.Id
            }).ToArrayAsync();

        return new PatientJourneyTree(paymentPlanJourneyTasks.ToArray(), patientTasks);
    }

    private async Task<PaymentPlanJourneyTaskModel[]> GetJourneyTreeData(int paymentPlanId, int practiceId)
    {
        var rawData = await _paymentPlanRepository.All()
            .Where(x => x.PaymentPlanId == paymentPlanId)
            .Select(x => new 
            {
                Id = x.Id!.Value,
                x.JourneyTaskId,
                x.PaymentPlanId,
                x.IsRequired,
                x.ParentId,
                x.Priority,
                x.JourneyTask.Title,
                x.JourneyTask.Description,
                x.JourneyTask.Group,
                x.JourneyTask.TaskCTA,
                x.JourneyTask.AutomaticCompletionPrerequisite
            })
            .ToArrayAsync();

        var settings = await _settingsManager.GetSettings(SettingNames, practiceId);
        var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];

        return rawData
            .Select(x => new PaymentPlanJourneyTaskModel
            {
                Id = x.Id,
                JourneyTaskId = x.JourneyTaskId,
                PaymentPlanId = x.PaymentPlanId,
                IsRequired = x.IsRequired,
                ParentId = x.ParentId,
                Priority = x.Priority,
                Title = x.Title,
                Description = x.Description,
                Group = x.Group,
                TaskCtaModel = GetTaskCtaModel(x.TaskCTA, x.Id, applicationUrl),
                AutomaticCompletionPrerequisite = x.AutomaticCompletionPrerequisite
            }).ToArray();
    }

    private JourneyTaskCTAModel? GetTaskCtaModel(string taskCtaJson, int id, string applicationUrl)
    {
        if (string.IsNullOrEmpty(taskCtaJson)) 
            return null;
        
        var taskCta = JsonConvert.DeserializeObject<JourneyTaskCTA>(taskCtaJson)!;
        return new JourneyTaskCTAModel
        {
            Title = taskCta.Title,
            Action = taskCta.Action,
            Link = JourneyTaskCTAModel.ExtractUrl(id, taskCta.LinkKey, _options, applicationUrl),
            FilePath = taskCta.FilePath
        };
    }
}