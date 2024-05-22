using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Context;
using WildHealth.Common.Models.Conversations;
using WildHealth.Infrastructure.Data.Queries.CustomSql;
using WildHealth.Infrastructure.Data.Queries.CustomSql.Models;


namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// <see cref="IConversationMessageUnreadNotificationService"/>
    /// </summary>
    public class ConversationParticipantMessageReadIndexService : IConversationParticipantMessageReadIndexService
    {

        private readonly IGeneralRepository<ConversationParticipantMessageReadIndex> _conversationParticipantMessageReadIndexRepository;
        private readonly ICustomSqlDataRunner _customSqlDataRunner;
        private readonly IApplicationDbContext _dbContext;

        public ConversationParticipantMessageReadIndexService(
            IGeneralRepository<ConversationParticipantMessageReadIndex> conversationParticipantMessageReadIndexRepository,
            ICustomSqlDataRunner customSqlDataRunner,
            IApplicationDbContext dbContext
        )
        {
            _conversationParticipantMessageReadIndexRepository = conversationParticipantMessageReadIndexRepository;
            _customSqlDataRunner = customSqlDataRunner;
            _dbContext = dbContext;
        }

        /// <summary>
        /// <see cref="IConversationMessageUnreadNotificationService.CreateAsync"/>
        /// </summary>
        /// <param name="conversationParticipantMessageReadIndex"></param>
        /// <returns></returns>
        public async Task<ConversationParticipantMessageReadIndex> CreateAsync(ConversationParticipantMessageReadIndex conversationParticipantMessageReadIndex)
        {
            await _conversationParticipantMessageReadIndexRepository.AddAsync(conversationParticipantMessageReadIndex);

            await _conversationParticipantMessageReadIndexRepository.SaveAsync();

            return conversationParticipantMessageReadIndex;
        }


        /// <summary>
        /// <see cref="IConversationMessageUnreadNotificationService.GetByConversationAsync(int)"/>
        /// </summary>
        /// <param name="conversationVendorExternalId"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public async Task<ConversationParticipantMessageReadIndex?> GetByConversationAndParticipantAsync(
            string conversationVendorExternalId, string participantVendorExternalId)
        {
            var model = await _conversationParticipantMessageReadIndexRepository
                .All()
                .ByConversationVendorExternalId(conversationVendorExternalId)
                .ByParticipantVendorExternalId(participantVendorExternalId)
                .FirstOrDefaultAsync();

            return model;
        }
        
        /// <summary>
        /// Get read index by conversationSid and user identity
        /// </summary>
        /// <param name="conversationVendorExternalId"></param>
        /// <param name="participantVendorExternalIdentity"></param>
        /// <returns></returns>
        public async Task<ConversationParticipantMessageReadIndex?> GetByConversationAndParticipantIdentityAsync(
            string conversationVendorExternalId, string participantVendorExternalIdentity)
        {
            var model = await _conversationParticipantMessageReadIndexRepository
                .All()
                .ByConversationVendorExternalId(conversationVendorExternalId)
                .ByParticipantVendorExternalId(participantVendorExternalIdentity)
                .FirstOrDefaultAsync();

            return model;
        }

        /// <summary>
        /// <see cref="IConversationParticipantMessageReadIndexService.GetByConversationAsync(int)"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ConversationParticipantMessageReadIndex> UpdateAsync(ConversationParticipantMessageReadIndex model)
        {
            _conversationParticipantMessageReadIndexRepository.Edit(model);
            
            await _conversationParticipantMessageReadIndexRepository.SaveAsync();

            return model;
        }

        public async Task<IEnumerable<ConversationParticipantMessageUnreadModel>> GetUnreadConversationParticipantIndexesWithoutNotifications() 
        {
            
            var results = new List<ConversationParticipantMessageUnreadModel>();
            
            var file = new FileInfo(Path.Combine(AppContext.BaseDirectory, "Queries/CustomSql/Sql/ConversationParticipantMessageReadIndex.sql"));
            
            var query = file.OpenText().ReadToEnd();

            await using(var command = _dbContext.Instance.Database.GetDbConnection().CreateCommand()) {
                command.CommandText = query;
                _dbContext.Instance.Database.OpenConnection();
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    if(reader.HasRows) {
                        while(reader.Read()) {
                            var model = new ConversationParticipantMessageUnreadModel(
                                conversationVendorExternalId: reader.GetString(0),
                                participantVendorExternalId: reader.GetString(1),
                                lastReadIndex: reader.GetInt32(2),
                                lastMessageSentDate: reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                createdAt: reader.GetDateTime(4),
                                modifiedAt: reader.IsDBNull(5) ? 
                                    (DateTime?)null :
                                    (DateTime?)reader.GetDateTime(4)
                            );
                            
                            results.Add(model);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// <see cref="IConversationParticipantMessageReadIndexService.GetUnreadMessagesFromParticularPatientAsync"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<EmployeeUnreadMessageFromParticularPatientsModel[]> GetUnreadMessagesFromParticularPatientAsync()
        {
            const string queryPath = "Queries/CustomSql/Sql/EmployeeUnreadMessagesCountFromParticularPatients.sql";

            var parameters = Array.Empty<CustomSqlDataParameter>();
            
            var result = await _customSqlDataRunner.GetDataSet<EmployeeUnreadMessageFromParticularPatientsModel>(queryPath, parameters);

            return result?.ToArray() ?? Array.Empty<EmployeeUnreadMessageFromParticularPatientsModel>();
        }
    }
}
