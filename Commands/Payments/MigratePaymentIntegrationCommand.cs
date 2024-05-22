using System.Collections.Generic;
using MediatR;
using WildHealth.Common.Models.Payments;

namespace WildHealth.Application.Commands.Payments;

public class MigratePaymentIntegrationCommand : IRequest<ICollection<PaymentPriceMigrationReport>>
{
    
}