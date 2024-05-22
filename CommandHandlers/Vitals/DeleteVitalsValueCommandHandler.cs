using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Vitals;
using WildHealth.Application.Services.Vitals;
using WildHealth.Domain.Entities.Vitals;

namespace WildHealth.Application.CommandHandlers.Vitals
{
    public class DeleteVitalsValueCommandHandler : IRequestHandler<DeleteVitalsValueCommand, ICollection<VitalValue>>
    {
        private readonly IVitalService _vitalService;
        public DeleteVitalsValueCommandHandler(IVitalService vitalService)
        {
            _vitalService = vitalService;
        }

        public async Task<ICollection<VitalValue>> Handle(DeleteVitalsValueCommand request, CancellationToken cancellationToken)
        {        
            return await _vitalService.DeleteVitalValuesAsync(request.PatientId, request.VitalsValuesIds);
        }

    }
}
