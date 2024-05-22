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
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Domain.Models.Patient;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Appointments
{
    public class PatientTypeResolverCommandHandler : IRequestHandler<PatientTypeResolverCommand, PatientType>
    {
        private readonly IPatientsService _patientsService;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;

        public PatientTypeResolverCommandHandler(
            IPatientsService patientsService,
            ILogger<PatientTypeResolverCommandHandler> logger,
            IMediator mediator)
        {
            _patientsService = patientsService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<PatientType> Handle(PatientTypeResolverCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientTypeResolver);

            var patientDomain = PatientDomain.Create(patient);

            return patientDomain.IsPremium ? PatientType.Premium : PatientType.Default;
        }
        
    }
}