using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Application.Commands.Products;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Appointments
{
    public class NoShowAppointmentCommandHandler : IRequestHandler<NoShowAppointmentCommand, Appointment>
    {
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IAppointmentsService _appointmentsService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public NoShowAppointmentCommandHandler(
            ILogger<NoShowAppointmentCommandHandler> logger,
            IFeatureFlagsService featureFlagsService,
            IAppointmentsService appointmentsService,
            IMediator mediator)
        {
            _featureFlagsService = featureFlagsService;
            _appointmentsService = appointmentsService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Appointment> Handle(NoShowAppointmentCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Mark appointment [Id] = {command.AppointmentId} as NoShow has been started.", command);

            var appointment = await _appointmentsService.GetByIdAsync(command.AppointmentId);

            appointment.IsNoShow = true;

            await _appointmentsService.EditAppointmentAsync(appointment);
        
            await VoidProductAsync(appointment.ProductId);

            await _mediator.Publish(new AppointmentNoShowEvent(appointment: appointment), cancellationToken);

            return appointment;
        }

        /// <summary>
        /// Void (un use) product
        /// </summary>
        /// <param name="productId"></param>
        private async Task VoidProductAsync(int? productId)
        {
            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.PatientProduct))
            {
                return;
            }
            
            if (productId is null)
            {
                return;
            }
            
            var command = new VoidProductCommand(productId.Value);

            await _mediator.Send(command);
        }
    }
}