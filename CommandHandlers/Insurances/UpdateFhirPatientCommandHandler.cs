using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.States;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class UpdateFhirPatientCommandHandler : IRequestHandler<UpdateFhirPatientCommand>
{
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly IPatientsService _patientsService;
    private readonly IStatesService _statesService;
    private readonly ILogger<UpdateFhirPatientCommandHandler> _logger;

    public UpdateFhirPatientCommandHandler(
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        IPatientsService patientsService,
        IStatesService statesService,
        ILogger<UpdateFhirPatientCommandHandler> logger)
    {
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _patientsService = patientsService;
        _statesService = statesService;
        _logger = logger;
    }

    public async Task Handle(UpdateFhirPatientCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Updating fhir patient with patient id = {command.PatientId} has started");

        var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientWithIntegrationsAndUser);

        var fhirPatientId = patient.User.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Customer)?.Value;

        if (string.IsNullOrEmpty(fhirPatientId))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Patient does not have insurance.");
        }

        await UpdateFhirPatient(fhirPatientId, patient.User);
        
        _logger.LogInformation($"Updating fhir patient with patient id = {command.PatientId} has finished");
    }

    #region private

    private async Task UpdateFhirPatient(string fhirPatientId, User user)
    {
        var stateAbbreviation = user.BillingAddress?.State;
        
        /*
         * Some users have the full state name set for their billing state.
         * OpenPM requires the abbreviation.
         */
        if (stateAbbreviation is not null && stateAbbreviation?.Length != 2)
        {
            var state = await _statesService.GetByName(stateAbbreviation!);
            stateAbbreviation = state.Abbreviation;
        }

        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(user.PracticeId);
        
        await pmService.UpdatePatientAsync(
            fhirPatientId: fhirPatientId,
            firstName: user.FirstName,
            lastName: user.LastName,
            birthday: user.Birthday ?? DateTime.Now,
            gender: user.Gender,
            phoneNumber: user.PhoneNumber,
            email: user.Email ?? string.Empty,
            streetAddress1: user.BillingAddress?.StreetAddress1 ?? string.Empty,
            streetAddress2: user.BillingAddress?.StreetAddress2 ?? string.Empty,
            city: user.BillingAddress?.City ?? string.Empty,
            state: stateAbbreviation ?? string.Empty,
            zipCode: user.BillingAddress?.ZipCode ?? string.Empty,
            practiceId: user.PracticeId
        );
    }

    #endregion
}