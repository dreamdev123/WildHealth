using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Vitals;
using WildHealth.Application.Services.Vitals;
using WildHealth.Domain.Entities.Vitals;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Vitals
{
    public class CreateVitalsWithValueCommandHandler : IRequestHandler<CreateVitalsWithValueCommand, ICollection<Vital>>
    {
        private readonly IVitalService _vitalService;

        public CreateVitalsWithValueCommandHandler(IVitalService vitalService)
        {
            _vitalService = vitalService;
        }

        public async Task<ICollection<Vital>> Handle(CreateVitalsWithValueCommand request, CancellationToken cancellationToken)
        {
            return await _vitalService.CreateAsync(request.PatientId, request.Vitals);
        }
    }
}
