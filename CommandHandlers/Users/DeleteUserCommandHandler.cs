using System;
using System.Collections.Generic;
using System.Threading;
using MediatR;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Services.Schedulers.Accounts;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Users;
using WildHealth.Infrastructure.Data.Queries.CustomSql;
using WildHealth.Settings;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Enums;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.CommandHandlers.Users
{

   public class DeleteUserCommandHandler : MessagingBaseService, IRequestHandler<DeleteUserCommand>
    {
        private readonly ILogger<DeleteUserCommandHandler> _logger;
        private readonly ICustomSqlDataRunner _customSqlDataRunner;
        private readonly IUsersService _usersService;
        private readonly IConversationsService _conversationsService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ISchedulerAccountService _schedulerAccountService;
        private readonly IGeneralRepository<Employee> _employeesRepository;


        public DeleteUserCommandHandler(
            IUsersService usersService,
            ILogger<DeleteUserCommandHandler> logger,
            ICustomSqlDataRunner customSqlDataRunner,
            IConversationsService conversationsService,
            ITwilioWebClient twilioWebClient,
            ISettingsManager settingsManager,
            IPermissionsGuard permissionsGuard,
            ISchedulerAccountService schedulerAccountService,
            IGeneralRepository<Employee> employeesRepository) : base(settingsManager)
        {
            _logger = logger;
            _customSqlDataRunner = customSqlDataRunner;
            _usersService = usersService;
            _conversationsService = conversationsService;
            _twilioWebClient = twilioWebClient;
            _permissionsGuard = permissionsGuard;
            _schedulerAccountService = schedulerAccountService;
            _employeesRepository = employeesRepository;
        }

        public async Task Handle(DeleteUserCommand command, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Running Delete command for [email] : {command.Email} with [procedure] : {command.StoreProcedureName}");

                var user = await _usersService.GetByEmailAsync(command.Email);
                
                _permissionsGuard.AssertAdminUser(user);
                
                // We only want to delete conversations if the user is a patient
                if (user!.Identity.Type == UserType.Patient)
                {
                    var credentials = await GetMessagingCredentialsAsync(user.PracticeId);

                    _twilioWebClient.Initialize(credentials);

                    var conversations = await _conversationsService.GetByParticipantEmail(command.Email);

                    foreach (var conversation in conversations)
                    {
                        var twilioConversation = await _twilioWebClient.GetConversationAsync(conversation.VendorExternalId);

                        await _twilioWebClient.DeleteConversationAsync(twilioConversation);
                    }
                }

                if (user!.Identity.Type == UserType.Employee)
                {
                    await RemoveEmployeeFromSchedulerAccountService(user);
                }

                var parameters = new List<SqlParameter>();
                
                parameters.Add(new SqlParameter("@email", command.Email));
                
                await _customSqlDataRunner.RunStoreProcedure<User>(command.StoreProcedureName,  parameters );

                _logger.LogInformation($"Delete command finished for [email]: {command.Email}");
            }
            catch (Exception err )
            {
                _logger.LogError($"Delete command failed for [email]: {command.Email} with [Error]: {err.ToString()}");
                throw new ApplicationException($"Delete command failed for [email]: {command.Email}", err);
            }
        }

        private async Task RemoveEmployeeFromSchedulerAccountService(User user)
        {
            var employee = user.Employee;
                    
            try {
                await _schedulerAccountService.DeleteAccountAsync(new DeleteSchedulerAccountModel() {
                    PracticeId = user.PracticeId,
                    AccountId = user.Employee.SchedulerAccountId
                });
            } catch(WildHealth.TimeKit.Clients.Exceptions.TimeKitException ex) {
                _logger.LogInformation($"Entity does not exist, continue on - {ex}");
            }

            employee.SchedulerAccountId = null;
                    
            _employeesRepository.Edit(employee);
                    
            await _employeesRepository.SaveAsync();
        }
    }
}