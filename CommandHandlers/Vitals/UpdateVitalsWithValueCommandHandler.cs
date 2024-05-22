using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Vitals;
using WildHealth.Application.Services.Vitals;
using WildHealth.Domain.Entities.Vitals;

namespace WildHealth.Application.CommandHandlers.Vitals
{
    public class UpdateVitalsWithValueCommandHandler : IRequestHandler<UpdateVitalsWithValueCommand, ICollection<VitalValue>>
    {
        private readonly IVitalService _vitalService;

        public UpdateVitalsWithValueCommandHandler(IVitalService vitalService)
        {
            _vitalService = vitalService;
        }

        public async Task<ICollection<VitalValue>> Handle(UpdateVitalsWithValueCommand request, CancellationToken cancellationToken)
        {
            return await _vitalService.UpdateVitalValueAsync(request.PatientId, request.Vitals);
        }
    }
}
