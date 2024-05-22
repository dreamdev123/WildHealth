using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;
using MediatR;
using WildHealth.Application.Services.Locations;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class ChangePatientsPracticeCommandHandler : IRequestHandler<ChangePatientsPracticeCommand, Patient>
    {
        private readonly ILogger<ChangePatientsPracticeCommandHandler> _logger;
        private readonly IPracticeService _practiceService;
        private readonly IPatientsService _patientsService;
        private readonly IUsersService _usersService;
        private readonly IAuthService _authService;
        private readonly ILocationsService _locationsService;
        
        public ChangePatientsPracticeCommandHandler(
            ILogger<ChangePatientsPracticeCommandHandler> logger,
            IPracticeService practiceService,
            IPatientsService patientsService,
            IUsersService usersService,
            IAuthService authService,
            ILocationsService locationsService)
        {
            _patientsService = patientsService;
            _practiceService = practiceService;
            _usersService = usersService;
            _authService = authService;
            _logger = logger;
            _locationsService = locationsService;
        }
        
        public async Task<Patient> Handle(ChangePatientsPracticeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Changing practice for patient with [Id] = {request.PatientId} to practice with [Id] = {request.NewPracticeId} has been started.");
            
            var patient = await _patientsService.GetByIdAsync(request.PatientId);
            var practice = await _practiceService.GetAsync(request.NewPracticeId);

            await UpdateUserAsync(patient.UserId, practice);

            await UpdateUserIdentityAsync(patient.UserId, practice);

            var defaultLocation = await _locationsService.GetDefaultLocationAsync(practice.GetId());
            
            patient.ChangeLocation(defaultLocation);

            await _patientsService.UpdateAsync(patient);

            _logger.LogInformation($"Changing practice for patient with [Id] = {request.PatientId} to practice with [Id] = {request.NewPracticeId} successfully completed.");
            
            return await _patientsService.GetByIdAsync(patient.GetId());
        }

        #region private

        private async Task UpdateUserAsync(int userId, Practice practice)
        {
            var user = await _usersService.GetAsync(userId);

            user.PracticeId = practice.GetId();
            await _usersService.UpdateAsync(user);
        }

        private async Task UpdateUserIdentityAsync(int userId, Practice practice)
        {
            await _authService.UpdatePracticeAsync(userId, practice);
        }
        
        #endregion
    }
}