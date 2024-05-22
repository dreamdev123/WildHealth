using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.PatientEngagements.Services;

public class PatientEngagementService : IPatientEngagementService
{
    private readonly EngagementCriteriaType[] _typesToDisableIfSplitImc = {
        EngagementCriteriaType.NoIMCForMoreThan6MonthsAfterCheckout,
        EngagementCriteriaType.NoICCForMoreThan2WeeksAfterCheckout,
        EngagementCriteriaType.NoIMCForMoreThan1DayAfterDNAAndLabsReturned,
        EngagementCriteriaType.NoIMCForMoreThan2WeeksAfterDNAAndLabsReturned,
        EngagementCriteriaType.IMCCompleted1DayAgo,
        EngagementCriteriaType.NoIMCForMoreThan1MonthAfterCheckout,
        EngagementCriteriaType.PremiumNoIMCForMoreThan1WeekAfterDNAAndLabsReturned,
        EngagementCriteriaType.PremiumNoIMCForMoreThan2MonthsAfterCheckout
    };
    
    private readonly EngagementCriteriaType[] _typesToDisableIfNotSplitImc = {
        EngagementCriteriaType.NoDnaReviewForMoreThan2MonthsAfterCheckout,
        EngagementCriteriaType.NoDnaReviewForMoreThan6MonthsAfterCheckout,
        EngagementCriteriaType.NoDnaReviewForMoreThan1DayAfterDNAAndLabsReturned,
        EngagementCriteriaType.NoDnaReviewForMoreThan2WeeksAfterDNAAndLabsReturned,
        EngagementCriteriaType.DnaReviewCompleted1DayAgo,
        EngagementCriteriaType.NoDnaReviewForMoreThan1MonthAfterCheckout,
        EngagementCriteriaType.PremiumNoDnaReviewForMoreThan1WeekAfterDNAAndLabsReturned,
        EngagementCriteriaType.PremiumNoDnaReviewForMoreThan2MonthsAfterCheckout
    };
    
    private readonly IGeneralRepository<PatientEngagement> _patientEngagementRepository;
    private readonly IGeneralRepository<EngagementCriteria> _criteriaRepository;
    private readonly IFeatureFlagsService _featureFlagsService;
    
    public PatientEngagementService(
        IGeneralRepository<PatientEngagement> patientEngagementRepository, 
        IGeneralRepository<EngagementCriteria> criteriaRepository,
        IFeatureFlagsService featureFlagsService)
    {
        _patientEngagementRepository = patientEngagementRepository;
        _criteriaRepository = criteriaRepository;
        _featureFlagsService = featureFlagsService;
    }

    public async Task<List<EngagementCriteria>> GetNotDisabledCriteria(EngagementAssignee assignee)
    {
        var criteria = await _criteriaRepository.All()
            .Where(x => !x.IsDisabled && (x.Assignee & assignee) == x.Assignee)
            .ToListAsync();
        
        // A temporary solution due to the impossibility of using the flag feature at the database level
        var typesToDisable = _featureFlagsService.GetFeatureFlag(FeatureFlags.SplitImc)
            ? _typesToDisableIfSplitImc
            : _typesToDisableIfNotSplitImc;
        
        return criteria.Where(x => !typesToDisable.Contains(x.Type)).ToList();
    }

    public async Task<List<PatientEngagement>> GetPending()
    {
        var result = _patientEngagementRepository.All()
            .Include(pe => pe.EngagementCriteria)
            .Include(pe => pe.Patient)
            .ThenInclude(p => p.User)
            .Where(pe => pe.Status == PatientEngagementStatus.PendingAction);
                         
        return await result.ToListAsync();
    }
    
    public async Task<List<PatientEngagement>> GetHistory(int[] patientIds)
    {
        var history = await _patientEngagementRepository
            .All()
            .Include(x => x.EngagementCriteria)
            .Where(x => patientIds.Contains(x.PatientId))
            .ToListAsync();

        return history;
    }

    public async Task<List<PatientEngagement>> GetActive(int[] patientIds, params EngagementCriteriaType[] types)
    {
        var result = _patientEngagementRepository.All()
            .Where(pe =>
                types.Contains(pe.EngagementCriteria.Type) &&
                (pe.Status == PatientEngagementStatus.PendingAction || pe.Status == PatientEngagementStatus.InProgress));
                         
        return await result.ToListAsync();
    }
}
