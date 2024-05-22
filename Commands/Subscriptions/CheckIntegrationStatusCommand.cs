using System.Collections.Generic;
using MediatR;
using WildHealth.Integration.Models.Subscriptions;

namespace WildHealth.Application.Commands.Subscriptions;

public class CheckIntegrationStatusCommand : IRequest<Dictionary<string, SubscriptionIntegrationModel[]>>
{
    public CheckIntegrationStatusCommand(int patientId)
    {
        PatientId = patientId;
    }

    public int PatientId { get; }
}