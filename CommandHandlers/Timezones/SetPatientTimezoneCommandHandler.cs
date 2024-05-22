using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Timezones;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.Timezones;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Timezones
{
    public class SetPatientTimezoneCommandHandler : IRequestHandler<SetPatientTimezoneCommand>
    {
        private readonly IPatientsService _patientsService;

        public SetPatientTimezoneCommandHandler(IPatientsService patientsService)
        {
            _patientsService = patientsService;
        }
        
        public async Task Handle(SetPatientTimezoneCommand request, CancellationToken cancellationToken)
        {
            var specification = PatientSpecifications.Empty;
            
            var patient = await _patientsService.GetByIdAsync(request.PatientId, specification);

            var timeZoneId = TimezoneHelper.GetWindowsId(request.Timezone);

            if (string.IsNullOrEmpty(timeZoneId) || patient.TimeZone == timeZoneId)
            {
                return;
            }
            
            patient.SetTimeZone(timeZoneId);
            
            await _patientsService.UpdateAsync(patient);
        }
    }
}