using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.PatientEngagements;

public interface IEngagementScannerAggregator
{
    IAsyncEnumerable<EngagementScannerAggregateResult> Aggregate(IEnumerable<IEngagementCriteriaScanner> scanners, DateTime timestamp);
}

public class EngagementScannerAggregator : IEngagementScannerAggregator
{
    private readonly IGeneralRepository<Subscription> _subscriptionRepository;
    private readonly ILogger<EngagementScannerAggregator> _logger;

    public EngagementScannerAggregator(
        IGeneralRepository<Subscription> subscriptionRepository, 
        ILogger<EngagementScannerAggregator> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }
    
    public async IAsyncEnumerable<EngagementScannerAggregateResult> Aggregate(IEnumerable<IEngagementCriteriaScanner> scanners, DateTime timestamp)
    {
        var source = _subscriptionRepository.All();
        foreach (var scanner in scanners)
        {
            var matched = await scanner.Scan(source, timestamp).ToListAsync().ToTry();
            matched.DoIfError(e => _logger.LogError(e, $"Couldn't scan for criteria: {scanner.Criteria().Name}"));
            
            foreach (var result in matched.ValueOr(EngagementScannerResult.Empty))
                yield return new EngagementScannerAggregateResult(result.PatientId, result.PatientUniversalId, result.IsPremium, scanner.Criteria());
        }
    }
}

public record EngagementScannerAggregateResult(int PatientId, Guid PatientUniversalId, bool IsPremium, EngagementCriteria Criteria);
