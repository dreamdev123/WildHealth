using MediatR;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Coverages;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Users;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances
{
    public class GetCoveragesCommandHandler : IRequestHandler<GetCoveragesCommand, Coverage[]>
    {
        private readonly ICoveragesService _coveragesService;
        private readonly IPatientsService _patientsService;
        private readonly IUsersService _usersService;

        public GetCoveragesCommandHandler(
            ICoveragesService coveragesService,
            IPatientsService patientsService,
            IUsersService usersService)
        {
            _coveragesService = coveragesService;
            _patientsService = patientsService;
            _usersService = usersService;
        }

        public async Task<Coverage[]> Handle(GetCoveragesCommand command, CancellationToken cancellationToken)
        {
            var user = await GetUserAsync(command);

            var coverages = await _coveragesService.GetAllAsync(user.GetId());

            return coverages;
        }

        #region private
        
        /// <summary>
        /// Fetches and returns user 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<User> GetUserAsync(GetCoveragesCommand command)
        {
            if (command.UserId.HasValue)
            {
                var specification = UserSpecifications.UserWithIntegrations;

                return await _usersService.GetAsync(command.UserId.Value, specification);
            }

            if (command.PatientId.HasValue)
            {
                var specification = PatientSpecifications.PatientWithIntegrations;

                var patient = await _patientsService.GetByIdAsync(command.PatientId.Value, specification);

                return patient.User;
            }

            throw new AppException(HttpStatusCode.BadRequest, "Patient is should be Greater Than than 0");
        }

        #endregion
    }
}
