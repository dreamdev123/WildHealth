using AutoMapper;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Users;
using WildHealth.Common.Models.Users;

namespace WildHealth.Application.CommandHandlers.Users
{
    public class FetchUserByEmailCommandHandler : IRequestHandler<FetchUserByEmailCommand, UserModel>
    {
        private readonly IMapper _mapper;
        private readonly IUsersService _usersService;
        
        public FetchUserByEmailCommandHandler(
            IMapper mapper,
            IUsersService usersService)
        {
            _mapper = mapper;
            _usersService = usersService;
        }

        public async Task<UserModel> Handle(FetchUserByEmailCommand command, CancellationToken cancellationToken)
        {
            var user = await _usersService.GetByEmailAsync(command.Email);
            
            var userModel = _mapper.Map<UserModel>(user);
            
            return userModel;
        }
    }
}