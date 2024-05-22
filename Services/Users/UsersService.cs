using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WildHealth.Application.Extensions.Query;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Common.Extensions;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Common.Models.Users;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Shared.Data.Extensions;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Shared.Data.Queries;
using WildHealth.Application.Utils.TokenGenerator;
using WildHealth.Common.Models.Auth;
using WildHealth.Domain.Entities.PhoneMetadata;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Services.Users
{
    /// <summary>
    /// <see cref="IUsersService"/>
    /// </summary>
    public class UsersService : IUsersService
    {
        private readonly IGeneralRepository<User> _usersRepository;
        private readonly IOptions<SystemStringOptions> _systemStringOptions;
        private readonly IConfirmCodeService _confirmCodeService;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IGeneralRepository<PhoneLookupRecord> _plrRepository;

        public UsersService(IGeneralRepository<User> usersRepository, 
            IGeneralRepository<PhoneLookupRecord> plrRepository,
            IOptions<SystemStringOptions> systemStringOptions, 
            ITokenGenerator tokenGenerator, 
            IConfirmCodeService confirmCodeService)
        {
            _usersRepository = usersRepository;
            _systemStringOptions = systemStringOptions;
            _tokenGenerator = tokenGenerator;
            _confirmCodeService = confirmCodeService;
            _plrRepository = plrRepository;
        }
        
        /// <summary>
        /// <see cref="IUsersService.GetAllAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var users = await _usersRepository
                .All()
                .IncludeEmployee()
                .IncludePatient()
                .ToListAsync();

            return users;
        }

        /// <summary>
        /// <see cref="IUsersService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<User?> GetByIdAsync(int id)
        {
            var user = await _usersRepository
                .Get(u => u.Id == id)
                .Include(x => x.Identity)
                .IncludeEmployee()
                .IncludePatient()
                .FirstOrDefaultAsync();
            
            return user;
        }

        /// <summary>
        /// <see cref="IUsersService.GetByPhoneAsync"/>
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public async Task<IEnumerable<User>> GetByPhoneAsync(string phone)
        {
            if(string.IsNullOrEmpty(phone))
            {
                return Enumerable.Empty<User>();
            }

            var searchPhone = phone.Trim();

            var lookups = _plrRepository.All().Where(x => x.E164PhoneNumber == searchPhone).ToList();

            if (!lookups.Any())
            {
                return Enumerable.Empty<User>();
            }

            var uids = lookups.Select(l => l.PhoneUserIdentity);
            var users = await _usersRepository.All()
                .Where(u => uids.Contains(u.UniversalId))
                .Include(u => u.Patient)
                .ToListAsync();
            
            return users;
        }

        /// <summary>
        /// <see cref="IUsersService.GetAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<User> GetAsync(int id)
        {
            var user = await _usersRepository
                .All()
                .Include(x => x.PreauthorizeRequest)
                .ById(id)
                .FirstOrDefaultAsync();
                
            if (user is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "User does not exist", exceptionParam);
            }

            return user;
        }

        /// <summary>
        /// <see cref="IUsersService.GetAsync(int, ISpecification{User})"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public async Task<User> GetAsync(int id, ISpecification<User> specification)
        {
            var user = await _usersRepository
                .All()
                .ById(id)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();
            
            if (user is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "User does not exist", exceptionParam);
            }

            return user;
        }

        /// <summary>
        /// <see cref="IUsersService.GetUsersByPermissionAsync(PermissionType)"/>
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public async Task<IEnumerable<User>> GetUsersByPermissionAsync(PermissionType permission)
        {
            return await _usersRepository
                .All()
                .IncludeIdentity()
                .IncludeEmployee()
                .ByPermission(permission)
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IUsersService.UpdateAsync(User)"/>
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<User> UpdateAsync(User user)
        {
            _usersRepository.Edit(user);
            
            await _usersRepository.SaveAsync();

            return user;
        }

        /// <summary>
        /// <see cref="IUsersService.GetByEmailAsync(string)"/>
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            var user = await _usersRepository
                .All()
                .IncludeIdentity()
                .IncludePatient()
                .IncludeEmployee()
                .IncludeIdentity()
                .FirstOrDefaultAsync(x => x.Email == email);

            return user;
        }

        /// <summary>
        /// Get user by email and include appropriate specification
        /// </summary>
        /// <param name="email"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<User> GetByEmailAsync(string email, ISpecification<User> specification)
        {
            var user = await _usersRepository
                .Get(u => u.Email == email)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();
            
            if (user is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(email), email);
                throw new AppException(HttpStatusCode.NotFound, "User does not exist", exceptionParam);
            }

            return user;
        }

        /// <summary>
        /// <see cref="IUsersService.GetByConversationIdentityAsync"/>
        /// </summary>
        /// <param name="conversationIdentity"></param>
        /// <returns></returns>
        public async Task<User?> GetByConversationIdentityAsync(string conversationIdentity)
        {
            var user = await _usersRepository
                .All()
                .IncludePatient()
                .IncludeIdentity()
                .FirstOrDefaultAsync(x => x.ConversationIdentity == conversationIdentity);

            return user;
        }

        #region helper

        public int? GetRoleId(UserIdentity identity)
        {
            return identity.Type == UserType.Employee
                ? identity.User.Employee.RoleId
                : new int?();
        }

         /// <summary>
        /// Determines and returns subscription status
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public SubscriptionStatus? DetermineSubscriptionStatus(UserIdentity identity)
        {
            var patient = identity.User.Patient;

            if (patient is null)
            {
                return SubscriptionStatus.Active;
            }

            return patient.MostRecentSubscription?.GetStatus()
                   ?? (patient.MostRecentSubscription != null && patient.MostRecentSubscription.CanBeActivated()
                       ? SubscriptionStatus.Deactivated
                       : SubscriptionStatus.PaymentFailed);
        }
         
        public Subscription? GetMostRecentSubscription(UserIdentity identity)
        {
            var patient = identity.User.Patient;

            if (patient != null)
            {
                return patient.MostRecentSubscription;
            }
            return null;
        }

        public DateTime? GetFirstSubscriptionStartDate(UserIdentity identity)
        {
            var patient = identity.User.Patient;

            if (patient != null && patient.Subscriptions.Any())
            {
                return patient.Subscriptions.Select(o => o.StartDate).Min();
            }
            
            return null;
        }

        public long? GetSubscriptionExpirationDate(UserIdentity identity)
        {
            var date = this.GetMostRecentSubscription(identity)?.GetEndDate();
       
            return date?.ToEpochTime();
        } 

        public string? GetTrackingIdentifier(UserIdentity identity)
        {
            return identity?.User?.TrackingIdentity();
        }

        /// <summary>
        /// Determines and returns pod for patient
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public string? GetAssignedPod(UserIdentity identity)
        {
            return identity.User.Patient?.Location?.Name;
        }

        /// <summary>
        /// Determines and returns provider for patient
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public string? GetAssignedProvider(UserIdentity identity)
        {
            return identity.User.Patient?.GetProvider()?.User?.GetFullname();
        }

        /// <summary>
        /// Determines and returns health coach for a patient
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public string? GetAssignedHealthCoach(UserIdentity identity)
        {
            return identity.User.Patient?.GetHealthCoach()?.User?.GetFullname();
        }

        /// <summary>
        /// Returns if all agreements confirmed
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public bool IsAgreementsConfirmed(UserIdentity identity)
        {
            var patient = identity.User.Patient;

            return patient is null || patient.IsAllAgreementsConfirmed();
        }

        /// <summary>
        /// Returns a User with UserGetMeModel all agreements confirmed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="tokenAsString"></param>
        /// <returns></returns>
        public async Task<UserGetMeModel> mapToUserGetMe(UserIdentity identity, string tokenAsString)
        {
            var roleId = GetRoleId(identity);
           
            var token = _tokenGenerator.Decrypt(tokenAsString);
            var subscriptionStatus = DetermineSubscriptionStatus(identity);
            var firstSubscriptionStartDate = GetFirstSubscriptionStartDate(identity);
            var currentSubscription = GetMostRecentSubscription(identity);
            var subscriptionExpires = GetSubscriptionExpirationDate(identity);
            var planName = currentSubscription?.GetPlanName();

            var confirmCode = await _confirmCodeService.GenerateAsync(identity.User, ConfirmCodeType.RefreshToken);

            var refreshTokenModel = new RefreshTokenModel 
            { 
                ExpirationDate = confirmCode.ExpireAt, 
                RefreshToken = confirmCode.Code 
            };

            
            
            return new UserGetMeModel {
                Id = identity.User.GetId(),
                EmployeeId = identity.User.Employee?.Id ?? 0,
                PatientId = identity.User.Patient?.Id ?? 0,
                PracticeId = identity.User.Practice.GetId(),
                Email = identity.User.Email,
                PhoneNumber = identity.User.PhoneNumber,
                IsVerified = identity.IsVerified,
                IsAgreementsConfirmed = IsAgreementsConfirmed(identity),
                FirstName = identity.User.FirstName,
                LastName = identity.User.LastName,
                SubscriptionStatus = subscriptionStatus,
                SubscriptionEndDate = Convert.ToInt64(subscriptionExpires),
                SubscriptionType = identity.User.SubscriptionType,
                RoleId = roleId,
                Created = firstSubscriptionStartDate ?? identity.User.CreatedAt,
                Plan = planName,
                Expires = token.ValidTo.ToEpochTime(),
                Token = tokenAsString,
                RefreshTokenModel = refreshTokenModel,
                DOB = identity.User.Birthday,
                Gender = identity.User.Gender,
                MeetingRecordingConsent = identity.User.Options.MeetingRecordingConsent
            };
        }

        public async Task<Guid> GetUserUniversalId(int patientId)
        {
            return await _usersRepository.All()
                .Where(u => u.Patient.Id == patientId)
                .Select(u => u.UniversalId)
                .FindAsync();
        }

        public async Task<User> GetByPatientIdAsync(int patientId)
        {
            return await _usersRepository.All()
                .FindAsync(u => u.Patient.Id == patientId);
        }

        public async Task<string> GetEmailAsync(int patientId)
        {
            return await _usersRepository.All()
                .Where(u => u.Patient.Id == patientId)
                .Select(u => u.Email)
                .FindAsync();
        }

        public async Task<string> GetUserName(int? userId)
        {
            var systemGeneratedName = _systemStringOptions.Value.SystemGeneratedName;

            if(userId == null)
            {
                return systemGeneratedName;
            }
        
            var user = await _usersRepository.GetAsync(userId.Value);

            if(user == null)
            {
                return systemGeneratedName;
            }
            
            return user.GetFullname();
        }

        #endregion
    }
}
