using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Shared.Enums;
using WildHealth.Application.Utils.PatientCreator;
using MediatR;
using WildHealth.Application.Services.Fellows;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class StartPatientRegistrationCommandHandler : IRequestHandler<StartPatientRegistrationCommand, Patient>
    {
        private readonly IMediator _mediator;
        private readonly IPatientCreator _patientCreator;
        private readonly IPatientsService _patientsService;
        private readonly ILocationsService _locationsService;
        private readonly IFellowsService _fellowsService;
        private readonly ILogger _logger;

        public StartPatientRegistrationCommandHandler(
            IMediator mediator,
            IPatientCreator patientCreator,
            IFellowsService fellowsService,
            IPatientsService patientsService,
            ILocationsService locationsService,
            ILogger<StartPatientRegistrationCommandHandler> logger)
        {
            _mediator = mediator;
            _fellowsService = fellowsService;
            _patientCreator = patientCreator;
            _patientsService = patientsService;
            _locationsService = locationsService;
            _logger = logger;
        }

        public async Task<Patient> Handle(StartPatientRegistrationCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var createInitialUserCommand = new CreateInitialUserCommand(
                    firstName: command.FirstName,
                    lastName: command.LastName,
                    email: command.Email,
                    phoneNumber: command.PhoneNumber,
                    birthday: command.Birthday,
                    userType: UserType.Patient,
                    gender: command.Gender,
                    practiceId: command.PracticeId);

                var user = await _mediator.Send(createInitialUserCommand, cancellationToken);

                var location = await _locationsService.GetFellowshipLocationAsync(command.PracticeId);

                var patientOptions = new PatientOptions
                {
                    IsFellow = true
                };

                var patient = await _patientCreator.Create(user, patientOptions, location);

                patient.SetRegistrationDate(DateTime.UtcNow);

                await _patientsService.CreatePatientAsync(patient);

                await AssignFellowAsync(patient, command.FellowId);

                await SendPracticumPatientAddedNotificationAsync(command.PracticeId, location.GetId());

                await SendPracticumPatientInvitationNotificationAsync(patient, command.FellowId);

                _logger.LogInformation($"Patient with [Id] = {patient.Id} initially registered successfully.");

                return patient;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Initial registration of patient with [Email] = {command.Email} was failed. {ex}");

                throw;
            }
        }

        private async Task SendPracticumPatientAddedNotificationAsync(int practiceId, int locationId)
        {
            var command = new SendPracticumPatientAddedNotificationCommand(practiceId, locationId);

            await _mediator.Send(command);
        }

        private async Task SendPracticumPatientInvitationNotificationAsync(Patient patient, int? fellowId)
        {
            if (!fellowId.HasValue)
            {
                return;
            }

            var command = new SendPracticumPatientInvitationEmailCommand(patient, fellowId.Value);

            await _mediator.Send(command);
        }

        private async Task AssignFellowAsync(Patient patient, int? fellowId)
        {
            if (!fellowId.HasValue)
            {
                return;
            }

            var fellow = await _fellowsService.GetByIdAsync(fellowId.Value);

            patient.FellowId = fellow.GetId();

            await _patientsService.UpdateAsync(patient);
        }
    }
}
