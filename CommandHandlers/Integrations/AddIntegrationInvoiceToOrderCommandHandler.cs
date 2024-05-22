using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Services.Integrations;
using WildHealth.Integration.Commands;

namespace WildHealth.Application.CommandHandlers.Integrations
{
    public class AddIntegrationInvoiceToOrderCommandHandler : IRequestHandler<AddIntegrationInvoiceToOrderCommand>
    {
        private readonly IIntegrationsService _integrationsService;

        public AddIntegrationInvoiceToOrderCommandHandler(IIntegrationsService integrationsService)
        {
            _integrationsService = integrationsService;
        }

        public async Task Handle(AddIntegrationInvoiceToOrderCommand request, CancellationToken cancellationToken)
        {
            await _integrationsService.CreateAsync(request.OrderInvoiceIntegration);
        }
    }
}
