using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Domain.ManualPayments;

public record ManualPaymentQueryFlow(IQueryable<Payment> Source, int ManualPaymentIdId) : IQueryFlow<ManualPaymentModel>
{
    public IQueryable<ManualPaymentModel> Execute()
    {
        return Source
            .Where(x => x.Id == ManualPaymentIdId)
            .Select(x => new ManualPaymentModel
            {
                Id = x.Id!.Value,
                Total = x.Total,
                Deposit = x.Deposit,
                DownPayment = x.DownPayment,
                RemainingPaidOverMonths = x.RemainingPaidOverMonths,
                // When it has a DownPayment then it's always the first ScheduleItem and we skip it for the UI  
                ScheduleItems = x.DownPayment > 0 ? x.ScheduleItems.OrderBy(x => x.Id).Skip(1).Select(y => new PaymentScheduleItemModel
                {
                    Amount = y.Amount,
                    DueDate = y.DueDate
                }).ToArray() : x.ScheduleItems.Select(y => new PaymentScheduleItemModel
                {
                    Amount = y.Amount,
                    DueDate = y.DueDate
                }).ToArray()
            });
    }
}