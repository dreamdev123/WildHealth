using System;
using MediatR;

namespace WildHealth.Application.Commands.Subscriptions
{
    public class ExecuteSubscriptionCancellationRequestsCommand : IRequest
    {
        public DateTime Date { get; }

        public ExecuteSubscriptionCancellationRequestsCommand(DateTime date)
        {
            Date = date;
        }
    }
}