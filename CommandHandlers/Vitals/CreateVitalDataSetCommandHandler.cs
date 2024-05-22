using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Vitals;
using WildHealth.Application.Services.Vitals;
using WildHealth.Domain.Entities.Vitals;

namespace WildHealth.Application.CommandHandlers.Vitals
{
    public class CreateVitalDataSetCommandHandler : IRequestHandler<CreateVitalDataSetCommand, ICollection<Vital>>
    {
        private readonly IVitalService _vitalService;

        public CreateVitalDataSetCommandHandler(IVitalService vitalService)
        {
            _vitalService = vitalService;
        }

        public async Task<ICollection<Vital>> Handle(CreateVitalDataSetCommand request, CancellationToken cancellationToken)
        {
            return await _vitalService.CreateVitalsValueDataSetAsync(request.PatientId, request.DateTime, request.SourceType);
        }
    }
}
