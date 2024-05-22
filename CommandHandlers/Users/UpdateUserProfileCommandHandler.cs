using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Events.Users;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Address;
using AutoMapper;

namespace WildHealth.Application.CommandHandlers.Users
{


    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, User>
    {
        private readonly IUsersService _usersService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ITransactionManager _transactionManager;
        private readonly IMediator _mediator;
        private readonly IAuthTicket _authTicket;
        private readonly IMapper _mapper;

        public UpdateUserProfileCommandHandler(
            IUsersService usersService,
            IPermissionsGuard permissionsGuard,
            ITransactionManager transactionManager,
            IMediator mediator,
            IAuthTicket authTicket,
            IMapper mapper)
        {
            _usersService = usersService;
            _permissionsGuard = permissionsGuard;
            _transactionManager = transactionManager;
            _mediator = mediator;
            _authTicket = authTicket;
            _mapper = mapper;
        }

        public async Task<User> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
        {
            var user = await GetUserAsync(command);

            _permissionsGuard.AssertUserPermissions(user);
            
            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                user = await UpdateUserAsync(user,command);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            var @event = new UserUpdatedEvent(user);
            await _mediator.Publish(@event, cancellationToken);
            return user;
        }

        #region private

        /// <summary>
        /// Fetches and returns user depends on identifier
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        private async Task<User> GetUserAsync(UpdateUserProfileCommand command)
        {
            var user = await _usersService.GetByEmailAsync(command.Email);

            if (user is null) throw new ApplicationException($"Failed to fetch User with [email] ${command.Email}");

            _permissionsGuard.AssertPermissions(user);
            
            return user;
        }


        private async Task<User> UpdateUserAsync(User user, UpdateUserProfileCommand command)
        {

            user.FirstName = command.FirstName;
            user.LastName = command.LastName;
            user.Birthday = command.Birthdate;
            user.Gender = command.Gender;
            user.PhoneNumber = command.PhoneNumber;
            user.ShippingAddress = command.ShippingAddress is not null ? _mapper.Map<Address>(command.ShippingAddress) : _mapper.Map<Address>(new AddressModel());
            user.BillingAddress = command.BillingAddress is not null ? _mapper.Map<Address>(command.BillingAddress) : _mapper.Map<Address>(new AddressModel());
            user.Options.MarketingSMS = command.SmsMarketing;
            user.Options.MeetingRecordingConsent = command.MeetingRecordingConsent;

            return await _usersService.UpdateAsync(user);

        }
        
        #endregion

    }
}