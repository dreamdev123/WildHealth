using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Models.Patient;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Patients;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class BindIntegrationIdByEmailCommandHandler : IRequestHandler<BindIntegrationIdByEmailCommand, Patient>
    {
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly IPatientsService _patientsService;
        private readonly IUsersService _usersService;

        public BindIntegrationIdByEmailCommandHandler(
            IIntegrationServiceFactory integrationServiceFactory,
            IPatientsService patientsService,
            IUsersService usersService)
        {
            _integrationServiceFactory = integrationServiceFactory;
            _patientsService = patientsService;
            _usersService = usersService;
        }
        
        public async Task<Patient> Handle(BindIntegrationIdByEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _usersService.GetByEmailAsync(request.Email);

            if (user is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "User is not found");
            }
            
            var patient = await _patientsService.GetByUserIdAsync(user.GetId());

            var patientDomain = PatientDomain.Create(patient);
            var integrationPatient = await GetIntegrationPatientAsync(patient);
            
            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);
            
            patientDomain.LinkWithIntegrationSystem(integrationPatient.Id, integrationService.IntegrationVendor);

            await _patientsService.UpdateAsync(patientDomain.Patient);

            return patientDomain.Patient;
        }

        private async Task<PatientIntegrationModel> GetIntegrationPatientAsync(Patient patient)
        {
            var practiceId = patient.User.PracticeId;
            
            var integrationService = await _integrationServiceFactory.CreateAsync(practiceId);

            var integrationPatients = await integrationService.MatchPatientsAsync(
                firstName: null,
                lastName: null,
                middleName: null,
                email: patient.User.Email);

            if (integrationPatients.Count() > 1)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Found 2 or more patients with the same email");
            }

            if (!integrationPatients.Any())
            {
                throw new AppException(HttpStatusCode.BadRequest, "No patients found by email");
            }

            return integrationPatients.First();
        }
    }
}