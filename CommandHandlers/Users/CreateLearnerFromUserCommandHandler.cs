using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Users;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Users;
using WildHealth.Northpass.Clients.Services;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.CommandHandlers.Users;

public class CreateLearnerFromUserCommandHandler : IRequestHandler<CreateLearnerFromUserCommand>
{
    private readonly ILogger<CreateLearnerFromUserCommandHandler> _logger;
    private readonly INorthpassService _northpass;
    private readonly IGeneralRepository<User> _usersRepository;

    public CreateLearnerFromUserCommandHandler (
        ILogger<CreateLearnerFromUserCommandHandler>  logger,
        INorthpassService northpass,
        IGeneralRepository<User> usersRepository)
    {
        _logger = logger;
        _northpass = northpass;
        _usersRepository = usersRepository;
    }

    public async Task Handle(CreateLearnerFromUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = request.User;

            // First see if the learner already exists in our system
            var northpassIntegration =
                user.GetIntegration(IntegrationVendor.Northpass, IntegrationPurposes.User.LmsId);

            if(northpassIntegration?.Value != null) {

                _logger.LogInformation($"Learner for Northpass already exists in our database for [UserId] = {user.Id}, [IntegrationId] = {northpassIntegration?.Value}");

                return;
            }

            // Next see if the learner already exists in their system
            var existingPeople = await _northpass.ListPeopleAsync(1, 100, user.Email);

            if(existingPeople.Data.Count() == 1) {

                var integrationId = existingPeople.Data.First().Id;

                _logger.LogInformation($"Learner in Northpass for [UserId] = {user.Id} already exists in Northpass, [IntegrationId] = {integrationId}, storing it in our database");

                await SaveUser(integrationId, user);

                return;
            }

            _logger.LogInformation($"Creating Northpass learner for user {user.Id}");

            var data = await _northpass.CreatePersonAsync(user.FirstName, user.LastName, user.Email, user.UniversalId.ToString());

            var id = data.Data.Id;

            await SaveUser(id, user);
            
            _logger.LogInformation($"Northpass learner created for user, integration id {id}");
        }
        catch (Exception e)
        {
            var m =
                $"A Northpass Learner record could not be created for user {request.User.Id}: {e.ToString()}";
            
            _logger.LogError(m);
        }
    }

    private async Task SaveUser(string integrationId, User user)
    {
        user.LinkWithIntegrationSystem(integrationId, IntegrationVendor.Northpass, IntegrationPurposes.User.LmsId);
        
        _usersRepository.Edit(user);

        await _usersRepository.SaveAsync();
    }
        
}