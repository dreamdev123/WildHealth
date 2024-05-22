using System;
using System.Threading.Tasks;
using WildHealth.Application.Domain.PatientEngagements.Flows;
using WildHealth.Application.Extensions.Query;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.PatientEngagements;

public interface IEngagementTaskCountCalculator
{
    Task<EngagementTasksCount> GetEngagementTaskCountForToday(int userId, DateTime today);
    Task<EngagementTasksCount> RaiseEngagementTaskCountForToday(int userId, int currentActiveTaskCount, DateTime today);
}

public class EngagementTaskCountCalculator : IEngagementTaskCountCalculator
{
    private readonly IGeneralRepository<UserSetting> _userSettingRepository;
    private readonly MaterializeFlow _materialiser;

    public EngagementTaskCountCalculator(
        IGeneralRepository<UserSetting> userSettingRepository, 
        MaterializeFlow materialiser)
    {
        _userSettingRepository = userSettingRepository;
        _materialiser = materialiser;
    }

    public async Task<EngagementTasksCount> GetEngagementTaskCountForToday(int userId, DateTime today)
    {
        var userSetting = await _userSettingRepository.All().FindAsync(x => 
            x.UserId == userId && 
            x.Key == EngagementTasksCount.Key &&
            x.CreatedAt.Date == today.Date).ToOption();
        
        return userSetting
            .Map(s => new EngagementTasksCount(int.Parse(s.Value)))
            .ValueOr(EngagementTasksCount.Default);
    }

    public async Task<EngagementTasksCount> RaiseEngagementTaskCountForToday(int userId, int currentActiveTaskCount, DateTime today)
    {
        var currentUserSetting = await _userSettingRepository.All().FindAsync(x => 
            x.UserId == userId && 
            x.Key == EngagementTasksCount.Key &&
            x.CreatedAt.Date == today.Date).ToOption();

        var newUserSetting = (await new RaiseEngagementTaskCountForTodayFlow(currentUserSetting, userId, currentActiveTaskCount)
            .Materialize(_materialiser))
            .Select<UserSetting>();
        
        return new EngagementTasksCount(int.Parse(newUserSetting.Value));
    }
}