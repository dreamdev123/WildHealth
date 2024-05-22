using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.PatientJourney;
using WildHealth.Domain.Models.Exceptions;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.PatientJourney;
using WildHealth.IntegrationEvents.PatientJourney.Payloads;

namespace WildHealth.Application.Domain.PatientJourney.Services;

public interface IPatientJourneyQueryService
{
    Task<List<TodoPatientJourneyTaskModel>> GetTodoTasks(int patientId, int paymentPlanId, int practiceId, int count);
    Task<PatientJourneyModel> GetPatientJourney(int patientId, int paymentPlanId, int practiceId);
    Task<(byte[], string)> GetAssetInBytes(int journeyTaskId, int patientId, int paymentPlanId, int practiceId);
}

public class PatientJourneyQueryService : IPatientJourneyQueryService
{
    private readonly IPatientJourneyTreeBuilder _journeyTreeBuilder;
    private readonly IAzureBlobService _azureBlobService;
    private readonly IEventBus _eventBus;
    private readonly IUsersService _usersService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PatientJourneyQueryService(
        IPatientJourneyTreeBuilder journeyTreeBuilder, 
        IAzureBlobService azureBlobService, 
        IEventBus eventBus, 
        IUsersService usersService, 
        IDateTimeProvider dateTimeProvider)
    {
        _journeyTreeBuilder = journeyTreeBuilder;
        _azureBlobService = azureBlobService;
        _eventBus = eventBus;
        _usersService = usersService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<List<TodoPatientJourneyTaskModel>> GetTodoTasks(int patientId, int paymentPlanId, int practiceId, int count)
    {
        var journeyTree = await _journeyTreeBuilder.Build(patientId, paymentPlanId, practiceId);

        journeyTree.ThrowIfEmpty();
        
        var todo = journeyTree.Todos().FirstOrDefault();
        if (todo is null)
            return new List<TodoPatientJourneyTaskModel>();

        return todo.RequiredTasks.Concat(todo.OptionalTasks)
            .Take(count)
            .ToList();
    }

    public async Task<PatientJourneyModel> GetPatientJourney(int patientId, int paymentPlanId, int practiceId)
    {
        var journeyTree = await _journeyTreeBuilder.Build(patientId, paymentPlanId, practiceId);
        
        journeyTree.ThrowIfEmpty();
        
        return new PatientJourneyModel
        {
            Todo = journeyTree.Todos(),
            Completed = journeyTree.Completed(),
            Rewards = journeyTree.Rewards()
        };
    }

    public async Task<(byte[], string)> GetAssetInBytes(int journeyTaskId, int patientId, int paymentPlanId, int practiceId)
    {
        var journeyTree = await _journeyTreeBuilder.Build(patientId, paymentPlanId, practiceId);

        var taskCta = journeyTree.GetTaskCTAById(journeyTaskId);
        if (taskCta is null || string.IsNullOrEmpty(taskCta?.FilePath))
            throw new EntityNotFoundException($"Couldn't find reward {journeyTaskId}");
        
        var bytes = await _azureBlobService.GetBlobBytes(AzureBlobContainers.Media, taskCta.FilePath);
        var rewardFileName = taskCta.FilePath.Replace("patient-journey-assets/", "");
        var universalId = await _usersService.GetUserUniversalId(patientId);
        
        await _eventBus.Publish(new PatientJourneyIntegrationEvent(new RewardDownloadedPayload(universalId.ToString(), rewardFileName), _dateTimeProvider.UtcNow()));
        
        return (bytes, rewardFileName);
    }
}