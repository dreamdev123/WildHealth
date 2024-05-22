using MediatR;

namespace WildHealth.Application.Commands.Users
{
    public class DeleteUserCommand : IRequest
    {
        public string Email { get; }
        
        public string StoreProcedureName { get; }

        public DeleteUserCommand(
            string email)
        {
            
            Email = email;
            StoreProcedureName = "RemoveUserByEmail";

        }
    }
}