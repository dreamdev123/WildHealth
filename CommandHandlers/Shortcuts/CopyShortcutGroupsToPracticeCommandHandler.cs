using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.ShortcutGroups;
using WildHealth.Application.Services.Practices;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Repository;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Shortcuts
{
    public class CopyShortcutGroupsToPracticeCommandHandler : IRequestHandler<CopyShortcutGroupsToPracticeCommand>
    {
        private readonly IPracticeService _practicesService;
        private readonly IGeneralRepository<ShortcutGroup> _shortcutGroupRepository;
        private readonly ILogger _logger;

        public CopyShortcutGroupsToPracticeCommandHandler(
            IPracticeService practicesService,
            IGeneralRepository<ShortcutGroup> shortcutGroupRepository,
            ILogger<CopyShortcutGroupsToPracticeCommandHandler> logger)
        {
            _practicesService = practicesService;
            _shortcutGroupRepository = shortcutGroupRepository;
            _logger = logger;
        }

        public async Task Handle(CopyShortcutGroupsToPracticeCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Copying shortcut groups from practice [Id] {command.FromPracticeId} to practice [Id] {command.ToPracticeId}.");

            var fromPractice = await _practicesService.GetSpecAsync(command.FromPracticeId, PracticeSpecifications.CopyPracticeShortcutsSpecification);

            var toPractice = await _practicesService.GetSpecAsync(command.ToPracticeId, PracticeSpecifications.CopyPracticeShortcutsSpecification);

            foreach(var shortcutGroup in fromPractice.ShortcutGroups) {

                var toPracticeShortcutGroup = this.GetToPracticeShortcutGroup(toPractice, shortcutGroup);

                // See if the group already exists
                if (toPracticeShortcutGroup == null)
                {

                    toPracticeShortcutGroup = new ShortcutGroup
                    {
                        Name = shortcutGroup.Name,
                        DisplayName = shortcutGroup.DisplayName,
                        PracticeId = command.ToPracticeId
                    };
                    
                    await _shortcutGroupRepository.AddAsync(toPracticeShortcutGroup);

                    await _shortcutGroupRepository.SaveAsync();

                    // Re-hydrate this object with shortcut group information
                    toPractice = await _practicesService.GetSpecAsync(command.ToPracticeId, PracticeSpecifications.CopyPracticeShortcutsSpecification);

                }

                foreach(var shortcut in shortcutGroup.Shortcuts) {

                    // First check to see if it already exists for the destination practice
                    if(!DoesDestinationPracticeAlreadyHaveShortcut(toPractice, shortcutGroup, shortcut)) {

                        var newShortcut = new Shortcut
                        {
                            Name = shortcut.Name,
                            DisplayName = shortcut.DisplayName,
                            Content = shortcut.Content,
                            GroupId = toPracticeShortcutGroup.GetId()
                        }; 
                        
                        await _shortcutGroupRepository.AddRelatedEntity(newShortcut);
                        
                        await _shortcutGroupRepository.SaveAsync();
                    }
                }
            }
        }

        private ShortcutGroup? GetToPracticeShortcutGroup(Practice practice, ShortcutGroup shortcutGroup) => 
            practice.ShortcutGroups.FirstOrDefault(o => o.Name == shortcutGroup.Name);

        private bool DoesDestinationPracticeAlreadyHaveShortcut(Practice toPractice, ShortcutGroup shortcutGroup, Shortcut shortcut) 
        {
            var practiceSg = toPractice.ShortcutGroups.FirstOrDefault(o => o.Name.Equals(shortcutGroup.Name));

            return practiceSg != null && practiceSg.Shortcuts.Any(o => 
                o.Name == shortcut.Name && 
                o.Content == shortcut.Content);
        }
    }
}