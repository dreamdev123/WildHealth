using System;
using MediatR;

namespace WildHealth.Application.Commands.Subscriptions
{
    public class RenewSubscriptionsCommand : IRequest
    {
        public DateTime Date { get; }

        public RenewSubscriptionsCommand(DateTime date)
        {
            Date = date;
        }
    }
}