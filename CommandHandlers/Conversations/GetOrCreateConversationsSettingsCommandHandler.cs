using System;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class GetOrCreateConversationsSettingsCommandHandler : IRequestHandler<GetOrCreateConversationsSettingsCommand, ConversationsSettings>
    {
        private readonly IConversationsSettingsService _conversationsSettingsService;

        public GetOrCreateConversationsSettingsCommandHandler(IConversationsSettingsService conversationsSettingsService)
        {
            _conversationsSettingsService = conversationsSettingsService;
        }

        public async Task<ConversationsSettings> Handle(GetOrCreateConversationsSettingsCommand command, CancellationToken cancellationToken)
        {
            var settings = await TryGetConversationsSettingsAsync(command);

            if (settings is null)
            {
                settings = new ConversationsSettings(command.EmployeeId);

                return await _conversationsSettingsService.CreateConversationsSettingsAsync(settings);
            }

            return settings;
        }

        #region private

        private async Task<ConversationsSettings?> TryGetConversationsSettingsAsync(GetOrCreateConversationsSettingsCommand command)
        {
            try
            {
                return await _conversationsSettingsService.GetByEmployeeIdLegacy(command.EmployeeId);
            }
            catch (AppException)
            {
                return null;
            }
        }

        #endregion
    }
}
