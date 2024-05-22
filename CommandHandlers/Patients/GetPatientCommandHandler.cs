using AutoMapper;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.Patients;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class GetPatientCommandHandler : IRequestHandler<GetPatientCommand, PatientModel>
    {
        private readonly IMapper _mapper;
        private readonly IPatientsService _patientsService;
        
        public GetPatientCommandHandler(
            IMapper mapper,
            IPatientsService patientsService)
        {
            _mapper = mapper;
            _patientsService = patientsService;
        }

        public async Task<PatientModel> Handle(GetPatientCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(command.Id);
            
            var patientModel = _mapper.Map<PatientModel>(patient);
            
            return patientModel;
        }
    }
}