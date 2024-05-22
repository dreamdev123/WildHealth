using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Fhir.Models.Hl7;
using WildHealth.Integration.Factories.IntegrationServiceFactory;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class CreateInsuranceChargeCommandHandler : IRequestHandler<CreateInsuranceChargeCommand, Unit>
{
    private readonly IPatientsService _patientsService;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly ILogger _logger;

    public CreateInsuranceChargeCommandHandler(
        IPatientsService patientsService, 
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory, 
        ILogger<CreateInsuranceChargeCommandHandler> logger)
    {
        _patientsService = patientsService;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _logger = logger;
    }

    public async Task<Unit> Handle(CreateInsuranceChargeCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Creating insurance charge for patient with [Id] = {command.PatientId} started.");

        var patient = await GetPatientAsync(command.PatientId);

        await AssertCanCreateChargeAsync(patient);

        var patientId = patient.User.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Customer).Value;
        
        var chargeModel = new CreateChargeHl7Model(
            patientId: patientId,
            chargeId: Guid.NewGuid().ToString(),
            price: command.Price
        );

        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(patient.User.PracticeId);

        await pmService.PublishMessageAsync(chargeModel, patient.User.PracticeId);
        
        _logger.LogInformation($"Creating insurance charge for patient with [Id] = {command.PatientId} finished.");

        return Unit.Value;
    }

    #region private

    /// <summary>
    /// Returns patient by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<Patient> GetPatientAsync(int id)
    {
        var specification = PatientSpecifications.PatientWithIntegrations;

        return await _patientsService.GetByIdAsync(id, specification);
    }

    /// <summary>
    /// Asserts if user can upload insurance
    /// </summary>
    private async Task AssertCanCreateChargeAsync(Patient patient)
    {
        if (patient.User.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Customer) is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Can't create insurance payment.");
        }
        
        if (patient.User.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Coverage) is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Can't create insurance payment.");
        }

        var coverageId = patient.User.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Coverage).Value;

        try
        {
            var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(patient.User.PracticeId);
            
            var coverage = await pmService.GetCoverageAsync(
                id: coverageId,
                practiceId: patient.User.PracticeId
            );
                
            if (coverage is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Can't create insurance payment.");
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning($"Create insurance payment has failed with [Error]: {e.ToString()}");
            throw new AppException(HttpStatusCode.BadRequest, "Can't create insurance payment.");
        }
    }
    
    #endregion
}