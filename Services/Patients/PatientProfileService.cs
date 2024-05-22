using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Extensions.Query;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Settings;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Patients;

public class PatientProfileService : IPatientProfileService
{
    private readonly AppOptions _appOptions;
    private readonly ISettingsManager _settingsManager;
    private readonly IGeneralRepository<Subscription> _subscriptionRepository;

    public PatientProfileService(
        IOptions<AppOptions> appOptions, 
        ISettingsManager settingsManager, 
        IGeneralRepository<Subscription> subscriptionRepository)
    {
        _appOptions = appOptions.Value;
        _settingsManager = settingsManager;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<string> GetProfileLink(int patientId, int practiceId)
    {
        var settings = await _settingsManager.GetSettings(new []{ SettingsNames.General.ApplicationBaseUrl }, practiceId);
        var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
        var patientProfileLink = string.Format(_appOptions.PatientProfileUrl, applicationUrl, patientId);
        return patientProfileLink;
    }
    
    public async Task<string> GetDashboardLink(int practiceId)
    {
        var settings = await _settingsManager.GetSettings(new []{SettingsNames.General.ApplicationBaseUrl}, practiceId);
        var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
        return string.Format(_appOptions.DashboardUrl, applicationUrl);
    }

    public async Task<MembershipInfo> GetMembershipInfo(int patientId, DateTime now)
    {
        return await _subscriptionRepository.All()
            .Where(x => x.PatientId == patientId && x.EndDate.Date >= now.Date && x.CanceledAt == null) // active subscription
            .Select(x => new MembershipInfo
            {
                PaymentPlanId = x.PaymentPrice.PaymentPeriod.PaymentPlanId,
                SubscriptionId = x.Id!.Value,
                PracticeId = x.PaymentPrice.PaymentPeriod.PaymentPlan.PracticeId
            }).FindAsync();
    }
}