using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Coverages;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class RunInsuranceVerificationCommandHandler : IRequestHandler<RunInsuranceVerificationCommand, InsuranceVerification[]>
{
    private readonly IPatientsService _patientsService;
    private readonly ICoveragesService _coveragesService;
    private readonly IMediator _mediator;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RunInsuranceVerificationCommandHandler> _logger;

    public RunInsuranceVerificationCommandHandler(
        IPatientsService patientsService,
        ICoveragesService coveragesService,
        IMediator mediator,
        IEventBus eventBus,
        ILogger<RunInsuranceVerificationCommandHandler> logger)
    {
        _patientsService = patientsService;
        _coveragesService = coveragesService;
        _mediator = mediator;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<InsuranceVerification[]> Handle(RunInsuranceVerificationCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Running insurance verification for patient id = {command.PatientId} has: started");
        
        var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientWithIntegrationsAndUser);

        var practiceId = patient.User.PracticeId;
        
        var coverages = await GetActiveCoverages(patient: patient);

        var hasValidInsurance = false;

        var verificationsRun = new List<InsuranceVerification>();
        
        foreach (var coverage in coverages)
        {
            try
            {
                var verification = await _mediator.Send(new VerifyCoverageCommand(coverageId: coverage.GetId()));
                
                if (verification.IsVerified)
                {
                    hasValidInsurance = true;
                }

                verificationsRun.Add(verification);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Running insurance verification for coverage id = {coverage.GetId()} has: failed to verify in practice management service {e}");
            }
        }

        if (!hasValidInsurance)
        {
            var payload = new FhirPatientCoverageVerificationPayload(isVerified: hasValidInsurance);

            await _eventBus.Publish(new FhirPatientIntegrationEvent(
                payload: payload,
                patient: new PatientMetadataModel(id: patient.GetId(), universalId: patient.UniversalId.ToString()),
                practice: new PracticeMetadataModel(id: practiceId),
                eventDate: DateTime.UtcNow));
        }

        _logger.LogInformation($"Running insurance verification for patient id = {command.PatientId} has: finished");

        return verificationsRun.ToArray();
    }

    #region private

    private async Task<Coverage[]> GetActiveCoverages(Patient patient)
    {
        var coverages = await _coveragesService.GetAllAsync(patient.UserId);
        
        return coverages.Where(o => o.Status == CoverageStatus.Active).ToArray();
    }

    #endregion
}