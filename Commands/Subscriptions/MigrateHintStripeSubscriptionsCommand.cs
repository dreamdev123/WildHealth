using System;
using MediatR;

namespace WildHealth.Application.Commands.Subscriptions
{
    public record MigrateHintStripeSubscriptionsCommand(int PatientId, 
        Action<string>? OutputDelegate = null, 
        bool IsTestMode = false) : IRequest;
}