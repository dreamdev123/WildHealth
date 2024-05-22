using System;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.DashboardAlerts;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Extensions;
using WildHealth.IntegrationEvents.DashboardAlerts;
using WildHealth.IntegrationEvents.DashboardAlerts.Payloads;

namespace WildHealth.Application.CommandHandlers.DashboardAlerts.Flows;

public class ResolveDashboardAlertFlow : IMaterialisableFlow
{
    private readonly DashboardAlert _alertToResolve;
    private readonly bool _isAssignedToEmployee;
    private readonly DateTime _utcNow;
    private readonly bool _isAssignedToCareCoordinator;
    private readonly string? _resolvedByUniversalId;

    public ResolveDashboardAlertFlow(
        DashboardAlert alertToResolve, 
        bool isAssignedToEmployee, 
        bool isAssignedToCareCoordinator,
        DateTime utcNow, 
        string? resolvedByUniversalId)
    {
        _alertToResolve = alertToResolve;
        _isAssignedToEmployee = isAssignedToEmployee;
        _isAssignedToCareCoordinator = isAssignedToCareCoordinator;
        _utcNow = utcNow;
        _resolvedByUniversalId = resolvedByUniversalId;
    }

    public MaterialisableFlowResult Execute()
    {
        if (string.IsNullOrEmpty(_resolvedByUniversalId)) return MaterialisableFlowResult.Empty;
        
        return (_isAssignedToEmployee, _isAssignedToCareCoordinator) switch
        {
            (false, false) => throw new DomainException("Can't resolve the alert because the patient isn't assigned to the employee."),
            _ => _alertToResolve.Deleted() + GetIntegrationEvent()
        };
    }

    private DashboardAlertIntegrationEvent GetIntegrationEvent()
    {
        return new DashboardAlertIntegrationEvent(new DashboardAlertResolvedPayload(_alertToResolve.Title.ToLower(), _resolvedByUniversalId), _utcNow);
    }
}