using AutoMapper;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Users;
using WildHealth.Common.Models.Users;

namespace WildHealth.Application.CommandHandlers.Users
{
    public class FetchUserCommandHandler : IRequestHandler<FetchUserCommand, UserModel>
    {
        private readonly IMapper _mapper;
        private readonly IUsersService _usersService;
        
        public FetchUserCommandHandler(
            IMapper mapper,
            IUsersService usersService)
        {
            _mapper = mapper;
            _usersService = usersService;
        }

        public async Task<UserModel> Handle(FetchUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _usersService.GetByConversationIdentityAsync(command.ConversationIdentity);
            
            var userModel = _mapper.Map<UserModel>(user);
            
            return userModel;
        }
    }
}