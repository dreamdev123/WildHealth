using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Common.Models.Conversations;
using WildHealth.Application.Services.Attachments;
using WildHealth.Domain.Enums.Attachments;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// <see cref="IScheduledMessagesService"/>
    /// </summary>
    /// <returns></returns>
    public class ScheduledMessagesService : IScheduledMessagesService
    {
        private readonly IGeneralRepository<ScheduledMessage> _scheduledMessagesRepository;
        private readonly IAttachmentsService _attachmentsService;

        public ScheduledMessagesService(
            IGeneralRepository<ScheduledMessage> scheduledMessagesRepository,
            IAttachmentsService attachmentsService
        )
        {
            _scheduledMessagesRepository = scheduledMessagesRepository;
            _attachmentsService = attachmentsService;
        }

        /// <summary>
        /// <see cref="IScheduledMessagesService.CreateAsync"/>
        /// </summary>
        /// <param name="scheduledMessage"></param>
        /// <returns></returns>
        public async Task<ScheduledMessage> CreateAsync(ScheduledMessage scheduledMessage)
        {
            await _scheduledMessagesRepository.AddAsync(scheduledMessage);

            await _scheduledMessagesRepository.SaveAsync();

            return scheduledMessage;
        }

        /// <summary>
        /// <see cref="IScheduledMessagesService.DeleteAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeleteAsync(int id)
        {
            var scheduledMessage = await GetByIdAsync(id);

            if (scheduledMessage is not null)
            {
                _scheduledMessagesRepository.Delete(scheduledMessage);
                await _scheduledMessagesRepository.SaveAsync();
            }
        }

        /// <summary>
        /// <see cref="IScheduledMessagesService.GetMessagesToSendAsync"/>
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ScheduledMessage>> GetMessagesToSendAsync(DateTime currentTime)
        {
            var result = await _scheduledMessagesRepository
                .All()
                .NotDeleted()
                .IncludeRelations()
                .IsNotSent()
                .ByTime(currentTime)
                .ToArrayAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IScheduledMessagesService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ScheduledMessage?> GetByIdAsync(int id)
        {
            var result = await _scheduledMessagesRepository
                .All()
                .ById(id)
                .Include(sm => sm.Conversation)
                .ThenInclude(conv => conv.PatientParticipants)
                .ThenInclude(pp => pp.Patient)
                .Include(sm => sm.Conversation)
                .ThenInclude(conv => conv.EmployeeParticipants)
                .ThenInclude(ep => ep.Employee)
                .FirstOrDefaultAsync();
            
            return result;
        }

        /// <summary>
        /// <see cref="IScheduledMessagesService.GetByEmployeeAsync"/>
        /// </summary>
        /// <param name="participantEmployeeId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ScheduledMessage>> GetByEmployeeAsync(int participantEmployeeId)
        {
            var result = await _scheduledMessagesRepository
                .All()
                .NotDeleted()
                .IncludeRelations()
                .ByParticipantEmployeeId(participantEmployeeId)
                .ToArrayAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IScheduledMessagesService.UpdateAsync"/>
        /// </summary>
        /// <param name="scheduledMessage"></param>
        /// <returns></returns>
        public async Task<ScheduledMessage> UpdateAsync(ScheduledMessage scheduledMessage)
        {
            _scheduledMessagesRepository.Edit(scheduledMessage);

            await _scheduledMessagesRepository.SaveAsync();

            return scheduledMessage;
        }

        /// <summary>
        /// <see cref="IScheduledMessagesService.GetMessagesByEmployeeAsync"/>
        /// </summary>
        /// <param name="participantEmployeeId"></param>
        /// <returns></returns>
        public async Task<ScheduledMessageModel[]> GetMessagesByEmployeeAsync(int participantEmployeeId)
        {
            var results = new List<ScheduledMessageModel>();
            
            var scheduledMessages = await _scheduledMessagesRepository
                .All()
                .NotDeleted()
                .IsNotSent()
                .IncludeRelations()
                .ByParticipantEmployeeId(participantEmployeeId)
                .ToArrayAsync();
                
            if (scheduledMessages.Length > 0)
            {
                foreach (var scheduledMessage in scheduledMessages)
                {
                    var attachments = await _attachmentsService.GetByTypeAttachmentAsync(AttachmentType.ScheduledMessageAttachment, scheduledMessage.GetId());
                    var model = new ScheduledMessageModel() {
                        Id = scheduledMessage.GetId(),
                        Message = scheduledMessage.Message,
                        TimeToSend = scheduledMessage.TimeToSend,
                        ConversationId = scheduledMessage.ConversationId,
                        ParticipantId = scheduledMessage.ConversationParticipantEmployeeId,
                        UploadedAttachments = attachments
                    };

                    results.Add(model);
                }
            }
            
            return results.ToArray();
            
        }
    }
}
