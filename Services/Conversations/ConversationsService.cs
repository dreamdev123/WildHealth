using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using WildHealth.Common.Models.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Infrastructure.Data.Queries.CustomSql;
using WildHealth.Infrastructure.Data.Queries.CustomSql.Models;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Extensions;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// <see cref="IConversationsService"/>
    /// </summary>
    public class ConversationsService: IConversationsService
    {
        private readonly IGeneralRepository<Conversation> _conversationsRepository;
        private readonly IGeneralRepository<ConversationParticipantEmployee> _conversationParticipantEmployeeRepository;
        private readonly IGeneralRepository<ConversationParticipantPatient> _conversationParticipantPatientRepository;
        private readonly ICustomSqlDataRunner _customSqlDataRunner;

        public ConversationsService(IGeneralRepository<Conversation> conversationsRepository, 
            IGeneralRepository<ConversationParticipantEmployee> conversationParticipantEmployeeRepository, 
            IGeneralRepository<ConversationParticipantPatient> conversationParticipantPatientRepository,
            ICustomSqlDataRunner customSqlDataRunner)
        {
            _conversationsRepository = conversationsRepository;
            _conversationParticipantEmployeeRepository = conversationParticipantEmployeeRepository;
            _conversationParticipantPatientRepository = conversationParticipantPatientRepository;
            _customSqlDataRunner = customSqlDataRunner;
        }

        /// <summary>
        /// Get unread message instances for all employees in a practice
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<EmployeeUnreadMessagesModel[]> EmployeeUnreadMessages(int practiceId)
        {
            var queryPath = "Queries/CustomSql/Sql/EmployeeUnreadMessages.sql";
            
            var parameters = new List<CustomSqlDataParameter>
            {
                new ()
                {
                    ParameterName = "@practiceId",
                    ParameterValue = practiceId,
                    DbType = DbType.Int32
                }
            };

            var results = await _customSqlDataRunner.GetDataSet<EmployeeUnreadMessagesModel>(queryPath, parameters);

            return results.ToArray();
        }
        
        /// <summary>
        /// Returns all conversations where the provider is active but the patient does NOT have an active subscription
        /// </summary>
        /// <returns></returns>
        public async Task<Conversation[]> HealthConversationsWithProviderAndPatientCancelled()
        {
            return await _conversationsRepository
                .All()
                .Active()
                .Include(o => o.PatientParticipants)
                    .ThenInclude(o => o.Patient)
                    .ThenInclude(o => o.Subscriptions)
                    .ThenInclude(o => o.Integrations)
                    .ThenInclude(o => o.Integration)
                .Include(o => o.EmployeeParticipants)
                    .ThenInclude(o => o.Employee)
                    .ThenInclude(o => o.Role)
                
                // Interested in active health conversation only
                .Where(o => o.Type == ConversationType.HealthCare && o.State == ConversationState.Active)
                
                // There's a provider active on the conversation and that provider is an active staff member
                .Where(o => o.EmployeeParticipants.Any(o =>
                    o.Employee.RoleId == WildHealth.Domain.Constants.Roles.ProviderId && o.DeletedAt == null))
                
                // All patient participants do NOT have an active subscription
                .Where(o => o.PatientParticipants.All(o => o.Patient.Subscriptions.All(a => a.CanceledAt != null || a.EndDate < DateTime.UtcNow)))

                .OrderByDescending(o => o.Id)
                
                .ToArrayAsync();
        }

        /// <summary>
        /// Returns all conversations where provider is active but health conversation is stale for XXX days and provider has read last message index
        /// </summary>
        /// <param name="daysStale"></param>
        /// <returns></returns>
        public async Task<Conversation[]> HealthConversationsWithProviderStaleForDays(int daysStale)
        {
            return await GetStaleConversations(
                roleName: WildHealth.Domain.Constants.Roles.ProviderDisplayName,
                daysStale: daysStale,
                conversationType: Convert.ToInt32(ConversationType.HealthCare));
        }

        /// <summary>
        /// Returns all conversations where provider is active but support conversation is stale for XXX days and provider has read last message index
        /// </summary>
        /// <param name="daysStale"></param>
        /// <returns></returns>
        public async Task<Conversation[]> SupportConversationsWithProviderStaleForDays(int daysStale)
        {
            return await GetStaleConversations(
                roleName: WildHealth.Domain.Constants.Roles.ProviderDisplayName,
                daysStale: daysStale,
                conversationType: Convert.ToInt32(ConversationType.Support));
        }

        /// <summary>
        /// <see cref="IConversationsService.GetAllActiveAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetAllActiveAsync()
        {
            var activeConversations = await _conversationsRepository
              .All()
              .Active()
              .IncludeParticipants()
              .ToListAsync();

            return activeConversations;
        }

        /// <summary>
        /// <see cref="IConversationsService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Conversation> GetByIdAsync(int id)
        {
            var conversation =  await _conversationsRepository
                .Get(x => x.Id == id)
                .IncludeParticipants()
                .IncludePatientLocation()
                .FirstOrDefaultAsync();

            if (conversation is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Conversation does not exist.", exceptionParam);
            }

            return conversation;
        }

        /// <summary>
        /// <see cref="IConversationsService.GetByParticipantIdentity"/>
        /// </summary>
        /// <param name="participantIdentity"></param>
        /// <returns></returns>
        public async Task<Conversation[]> GetByParticipantIdentity(string participantIdentity)
        {
            var employeeConversations = await _conversationsRepository
                .Get(o => o.EmployeeParticipants.Select(o => o.VendorExternalIdentity).Contains(participantIdentity))
                .ToArrayAsync();

            if (employeeConversations.Any())
            {
                return employeeConversations;
            }
            var patientConversations = await _conversationsRepository
                .Get(o => o.PatientParticipants.Select(o => o.VendorExternalIdentity).Contains(participantIdentity))
                .ToArrayAsync();

            return patientConversations;
        }

        /// <summary>
        /// WARNING: Do NOT use this method for performance reasons. <see cref="IConversationsService.GetByExternalVendorIdAsync(string)"/>
        /// </summary>
        /// <param name="vendorExternalId"></param>
        /// <param name="isTracking"></param>
        /// <returns></returns>
        public async Task<Conversation> GetByExternalVendorIdAsync(string vendorExternalId, bool isTracking = false)
        {
            var conversationByExternalVendor =  _conversationsRepository
                .All()
                .ByVendorExternalId(vendorExternalId)
                .IncludeParticipants();

            if (!isTracking)
            {
                conversationByExternalVendor = conversationByExternalVendor.AsNoTracking();
            }

            var result = await conversationByExternalVendor
                .FirstOrDefaultAsync();

            if (result is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(vendorExternalId), vendorExternalId);
                throw new AppException(HttpStatusCode.NotFound, "Conversation by external vendor does not exist.", exceptionParam);
            }

            return result;
        }
        
        /// <summary>
        /// <see cref="IConversationsService.GetByParticipantEmail(string)"/>
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<Conversation[]> GetByParticipantEmail(string email)
        {
            var patientConversations = await _conversationsRepository
                .All()
                .Where(o => o.PatientParticipants.Select(o => o.Patient.User.Email).Contains(email))
                .ToArrayAsync();

            if (patientConversations.Any())
            {
                return patientConversations;
            }
            
            var employeeConversations = await _conversationsRepository
                .All()
                .Where(o => o.EmployeeParticipants.Where(x=> !x.DeletedAt.HasValue).Select(o => o.Employee.User.Email).Contains(email))
                .ToArrayAsync();

            if (employeeConversations.Any())
            {
                return employeeConversations;
            }

            return Enumerable.Empty<Conversation>().ToArray();
        }

        /// <summary>
        /// <see cref="IConversationsService.GetByExternalVendorIdTrackAsync(string)"/>
        /// </summary>
        /// <param name="vendorExternalId"></param>
        /// <returns></returns>
        public async Task<Conversation> GetByExternalVendorIdTrackAsync(string vendorExternalId)
        {
            var conversationByExternalVendor =  await _conversationsRepository
                .All()
                .ByVendorExternalId(vendorExternalId)
                .Where(x=> x.EmployeeParticipants.Any(c=> !c.DeletedAt.HasValue))
                .IncludeParticipants()
                .FirstOrDefaultAsync();

            if (conversationByExternalVendor is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(vendorExternalId), vendorExternalId);
                throw new AppException(HttpStatusCode.NotFound, "Conversation by external vendor does not exist.", exceptionParam);
            }

            return conversationByExternalVendor;
        }

        public async Task<Conversation> GetByExternalVendorIdSpecAsync(string vendorExternalId, ISpecification<Conversation> spec)
        {
            var query = _conversationsRepository
                .All()
                .ByVendorExternalId(vendorExternalId)
                .ApplySpecification(spec);
            
            return await query.FirstAsync();
        }

        /// <summary>
        /// <see cref="IConversationsService.GetSupportConversationsByPatientAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetSupportConversationsByPatientAsync(int patientId)
        {
            var supportConversationByPatient = await _conversationsRepository
                .All()
                .RelatedToPatient(ConversationType.Support, patientId)
                .IncludeParticipants()
                .IncludeStateChangeEmployee()
                .AsNoTracking()
                .ToListAsync();

            if (supportConversationByPatient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Support conversation by patient does not exist.", exceptionParam);
            }

            return supportConversationByPatient;
        }

        /// <summary>
        /// <see cref="IConversationsService.GetHealthConversationByPatientAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<Conversation> GetHealthConversationByPatientAsync(int patientId)
        {
            var healthConversationByPatient =  await _conversationsRepository
                .All()
                .RelatedToPatient(ConversationType.HealthCare, patientId)
                .IncludeParticipants()
                .IncludePatientLocation()
                .FirstOrDefaultAsync();

            if (healthConversationByPatient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Health conversation by patient does not exist.", exceptionParam);
            }

            return healthConversationByPatient;
        }
        
        /// <summary>
        /// <see cref="IConversationsService.GetAllConversationByPatientAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<List<Conversation>> GetAllConversationByPatientAsync(int patientId)
        {
            var conversations =  await _conversationsRepository
                .All()
                .RelatedToPatient(null, patientId)
                .IncludeParticipants()
                .ToListAsync();

            if (conversations is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Conversation by patient does not exist.", exceptionParam);
            }

            return conversations;
        }

        /// <summary>
        /// <see cref="IConversationsService.GetConversationsByEmployeeAsync"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="isActive"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetConversationsByEmployeeAsync(int employeeId, bool isActive = false)
        {
            var specification = ConversationSpecifications.ParticipantsAndLocations;

            return await GetConversationsByEmployeeAsync(employeeId, specification, isActive);
        }

        /// <summary>
        /// <see cref="IConversationsService.GetConversationsByEmployeeAsync"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="specification"></param>
        /// <param name="isActive"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetConversationsByEmployeeAsync(int employeeId,
            ISpecification<Conversation> specification, bool isActive = false)
        {
            var query = _conversationsRepository
                .All()
                .RelatedToEmployee(employeeId)
                .ApplySpecification(specification)
                .IncludeStateChangeEmployee();

            if (isActive)
            {
                query = query.Active();
            }
                
            var conversation = await query
                .AsNoTracking()
                .ToListAsync();
            
            return conversation;
        }

        /// <summary>
        /// <see cref="IConversationsService.GetDelegatedConversationsByEmployeeAsync"/>
        /// </summary>
        /// <param name="delegatedTo"></param>
        /// <param name="delegatedBy"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetDelegatedConversationsByEmployeeAsync(int delegatedTo, int delegatedBy)
        {
            var conversation = await _conversationsRepository
                .All()
                .DelegatedToEmployee(delegatedTo, delegatedBy)
                .IncludeParticipants()
                .IncludePatientLocation()
                .AsNoTracking()
                .ToListAsync();

            return conversation;
        }

        /// <summary>
        /// <see cref="IConversationsService.UpdateConversationAsync"/>
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        public async Task<Conversation> UpdateConversationAsync(Conversation conversation)
        {
            _conversationsRepository.Edit(conversation);
            
            await _conversationsRepository.SaveAsync();

            return conversation;
        }
     
        /// <summary>
        /// <see cref="IConversationsService.CreateConversationAsync"/>
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        public async Task<Conversation> CreateConversationAsync(Conversation conversation)
        {
            await _conversationsRepository.AddAsync(conversation);
            
            await _conversationsRepository.SaveAsync();

            return conversation;
        }

        /// <summary>
        /// <see cref="IConversationsService.AddParticipantAsync"/>
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="employee"></param>
        /// <returns></returns>
        public async Task<Conversation> AddParticipantAsync(Conversation conversation, ConversationParticipantEmployee employee)
        {
            var conversationDomain = ConversationDomain.Create(conversation);
            conversationDomain.AddEmployeeParticipant(employee);

            _conversationsRepository.Edit(conversation);

            await _conversationsRepository.SaveAsync();

            return conversation;
        }

        /// <summary>
        /// <see cref="IConversationsService.GetSupportSubmissionsAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetSupportSubmissionsAsync(int[] locationIds)
        {
            var conversationsOpenSupport = await _conversationsRepository
               .All()
               .IncludeParticipants()
               .RelatedToLocations(locationIds)
               .Support()
               .IncludeOpened()
               .AsNoTracking()
               .ToArrayAsync();

            return conversationsOpenSupport;
        }

        /// <summary>
        /// <see cref="IConversationsService.RemoveParticipantAsync"/>
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="employee"></param>
        /// <returns></returns>
        public async Task<Conversation> RemoveParticipantAsync(Conversation conversation, ConversationParticipantEmployee employee)
        {
            employee.DeletedAt = DateTime.UtcNow;
            
            employee.SetVendorExternalId(null);
            
            _conversationParticipantEmployeeRepository.EditRelated(employee);

            await _conversationParticipantEmployeeRepository.SaveAsync();

            return conversation;
        }

        /// <summary>
        /// <see cref="IConversationsService.GetAllActiveSupportAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetAllActiveSupportAsync()
        {
            var activeSupportConversations = await _conversationsRepository
             .All()
             .Active()
             .Support()
             .IncludeParticipants()
             .ToListAsync();

            return activeSupportConversations;
        }

        /// <summary>
        /// <see cref="IConversationsService.GetAllActiveHealthAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetAllActiveHealthAsync()
        {
            var activeHealthConversations = await _conversationsRepository
                .All()
                .Active()
                .Health()
                .IncludeParticipants()
                .AsNoTracking()
                .ToListAsync();

            return activeHealthConversations;
        }

        /// <summary>
        /// All health conversations with a message sent since the given date
        /// </summary>
        /// <param name="since"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Conversation>> GetAllActiveWithMessageSentSince(DateTime since)
        {
            var activeHealthConversations = await _conversationsRepository
                .All()
                .Active()
                .LastMessageSentSince(since)
                .IncludeParticipants()
                .AsNoTracking()
                .ToListAsync();

            return activeHealthConversations;
        }

        public async Task<(IEnumerable<ConversationParticipantEmployee>, IEnumerable<ConversationParticipantPatient>)> GetConversationParticipants(int conversationId)
        {
            var employees = await _conversationParticipantEmployeeRepository.All()
                .Where(c => c.ConversationId == conversationId)
                .Include(x => x.Employee)
                .ThenInclude(x => x.User)
                .ThenInclude(x => x.Devices)
                .AsNoTracking()
                .ToListAsync();
            
            var patients = await _conversationParticipantPatientRepository.All()
                .Where(c => c.ConversationId == conversationId)
                .Include(x => x.Patient)
                .ThenInclude(x => x.User)
                .ThenInclude(x => x.Devices)
                .AsNoTracking()
                .ToListAsync();

            return (employees, patients);
        }
        
        /// <summary>
        /// Returns unread message information about any conversations that the employee should be responsible for based on conversation settings 
        /// </summary>
        /// <param name="forwardingToEmployeeId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ForwardingConversationWithUnreadMessageModel>> GetForwardingConversationsWithUnreadMessages(int forwardingToEmployeeId) 
        {
            var queryPath = "Queries/CustomSql/Sql/ForwardingConversationsWithUnreadMessages.sql";
            
            var parameters = new List<CustomSqlDataParameter>
            {
                new ()
                {
                    ParameterName = "@employeeId",
                    ParameterValue = forwardingToEmployeeId.ToString(),
                    DbType = DbType.String
                }
            };

            return await _customSqlDataRunner.GetDataSet<ForwardingConversationWithUnreadMessageModel>(queryPath, parameters);
        }


        private async Task<Conversation[]> GetStaleConversations(string roleName, int daysStale, int conversationType)
        {
            var queryPath = "Queries/CustomSql/Sql/ConversationsWithRoleStaleForDays.sql";
            
            var parameters = new List<CustomSqlDataParameter>
            {
                new ()
                {
                    ParameterName = "@daysStale",
                    ParameterValue = daysStale,
                    DbType = DbType.Int32
                },
                new ()
                {
                    ParameterName = "@roleName",
                    ParameterValue = roleName,
                    DbType = DbType.String
                },
                new ()
                {
                    ParameterName = "@conversationType",
                    ParameterValue = conversationType,
                    DbType = DbType.Int32
                }
            };

            var results = await _customSqlDataRunner.GetDataSet<ConversationsWithRoleStaleForDaysModel>(queryPath, parameters);

            var conversationIds = results.Select(o => o.ConversationId);
            
            return await _conversationsRepository
                .All()
                .Include(o => o.PatientParticipants)
                .ThenInclude(o => o.Patient)
                .ThenInclude(o => o.Subscriptions)
                .ThenInclude(o => o.Integrations)
                .ThenInclude(o => o.Integration)
                .Include(o => o.EmployeeParticipants)
                .ThenInclude(o => o.Employee)
                .ThenInclude(o => o.Role)
                .Where(o => o.Id != null && conversationIds.Contains(o.Id.Value))
                .ToArrayAsync();
        }
    }
}
