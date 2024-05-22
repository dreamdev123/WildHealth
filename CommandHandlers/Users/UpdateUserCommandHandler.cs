using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Entities.Users;
using AutoMapper;
using MediatR;
using WildHealth.Application.CommandHandlers.Users.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Address;

namespace WildHealth.Application.CommandHandlers.Users
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, User>
    {
        private readonly IUsersService _usersService;
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        private readonly MaterializeFlow _materializeFlow;

        public UpdateUserCommandHandler(
            IUsersService usersService,
            IAuthService authService, 
            IMapper mapper, MaterializeFlow materializeFlow)
        {
            _usersService = usersService;
            _authService = authService;
            _mapper = mapper;
            _materializeFlow = materializeFlow;
        }

        public async Task<User> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _usersService.GetAsync(command.Id);
            var identity = await _authService.GetByIdAsync(user.GetId());
            var emailIsInUse = await _authService.CheckIfEmailExistsAsync(command.Email);
            
            return (await new UpdateUserFlow(
                user,
                command.FirstName,
                command.LastName,
                command.PhoneNumber,
                command.Birthday,
                command.Gender,
                _mapper.Map<Address>(command.BillingAddress),
                _mapper.Map<Address>(command.ShippingAddress),
                identity,
                command.Type,
                emailIsInUse,
                command.Email,
                command.IsRegistrationCompleted).Materialize(_materializeFlow)).Select<User>();
        }
    }
}