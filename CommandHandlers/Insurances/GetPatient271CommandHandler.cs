using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Insurances.Flows;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Services.Coverages;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class GetPatient271CommandHandler : IRequestHandler<GetPatient271Command, string>
{
    private readonly IPatientsService _patientsService;
    private readonly ICoveragesService _coveragesService;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly ILogger<GetPatient271CommandHandler> _logger;

    public GetPatient271CommandHandler(
        IPatientsService patientsService,
        ICoveragesService coveragesService,
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        ILogger<GetPatient271CommandHandler> logger)
    {
        _patientsService = patientsService;
        _coveragesService = coveragesService;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _logger = logger;
    }

    public async Task<string> Handle(GetPatient271Command command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Getting 271 for patient id = {command.PatientId} has: started");
        
        var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientWithIntegrationsAndUser);

        var practiceId = patient.User.PracticeId;
        
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(practiceId: practiceId);

        var pmPatientId = GetPmPatientId(patient: patient, vendor: pmService.Vendor);

        var coverages = await GetActiveCoverages(patient: patient);

        var serviceTypes = new[] { InsuranceConfigurationConstants.Rte.ServiceTypes.PlanCoverage };
        
        foreach (var coverage in coverages)
        {
            try
            {
                var eligibility = await pmService.GetEligibilityAsync(
                    pmPatientId: pmPatientId,
                    policyNumber: coverage.MemberId,
                    serviceTypes: serviceTypes,
                    return271: true,
                    practiceId: practiceId);

                var raw271 = eligibility?.Raw271;
                
                if (!string.IsNullOrEmpty(raw271))
                {
                    _logger.LogInformation($"Getting 271 patient id = {command.PatientId} has: found 271 for coverage id = {coverage.GetId()}");

                    return raw271;
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Getting 271 for coverage id = {coverage.GetId()} has: failed {e}");
            }
        }
        
        throw new AppException(HttpStatusCode.NotFound,$"271 not found for patient id = {patient.GetId()}");
    }
    
    #region private

    private string GetPmPatientId(Patient patient, IntegrationVendor vendor)
    {
        var pmPatientId = patient.User.GetIntegrationId(vendor, IntegrationPurposes.User.Customer);

        if (pmPatientId is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"pmPatientId not found for patient id = {patient.GetId()}");
        }

        return pmPatientId;
    }

    private async Task<Coverage[]> GetActiveCoverages(Patient patient)
    {
        var coverages = await _coveragesService.GetAllAsync(patient.UserId);
        
        return coverages.Where(o => o.Status == CoverageStatus.Active).ToArray();
    }

    #endregion
}