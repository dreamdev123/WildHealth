using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.Commands.Patients
{
    public class SynchronizePatientStatusWithOrderStatusCommand : IRequest
    {
        public Order Order { get; }

        public SynchronizePatientStatusWithOrderStatusCommand(Order order)
        {
            Order = order;
        }
    }
}
