using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Inputs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class RefreshLabInputValueRangeCalculationsCommandHandler : IRequestHandler<RefreshLabInputValueRangeCalculationsCommand>
    {
        private readonly IGeneralRepository<InputsAggregator> _iaRepository;

        public RefreshLabInputValueRangeCalculationsCommandHandler(
            IGeneralRepository<InputsAggregator> iaRepository)
        {
            _iaRepository = iaRepository;
        }

        public async Task Handle(RefreshLabInputValueRangeCalculationsCommand command, CancellationToken cancellationToken)
        {
            var ia = await _iaRepository
                .All()
                .Where(o => o.PatientId == command.PatientId)
                .Include(o => o.LabInputValues).ThenInclude(o => o.LabInput)
                .FindAsync();

            foreach(var liv in ia.LabInputValues)
            {
                if (command.OnDate is not null && command.OnDate != liv.Date)
                {
                    continue;
                }
                
                liv.Validate();
            }
            
            _iaRepository.Edit(ia);
    
            await _iaRepository.SaveAsync();
        }
    }
}



