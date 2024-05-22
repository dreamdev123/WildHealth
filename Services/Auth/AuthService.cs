using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Security.Claims;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.HealthScore;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Employees;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Data.Extensions;
using WildHealth.Application.Utils.TokenGenerator;
using WildHealth.Application.Utils.PasswordUtil;
using WildHealth.Application.Services.Users;
using WildHealth.ClarityCore.Exceptions;
using WildHealth.ClarityCore.Models.HealthScore;
using WildHealth.Common.Extensions;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Common.Models.Auth;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Enums.User;
using WildHealth.IntegrationEvents.Users.Payloads;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Constants;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Utils.CryptographyUtil;
using MediatR;
using WildHealth.Application.Extensions.Query;

namespace WildHealth.Application.Services.Auth
{
    /// <summary>
    /// <see cref="IAuthService"/>
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IGeneralRepository<ExternalAuthorization> _externalAuthorizationRepository;
        private readonly IGeneralRepository<UserIdentity> _identityRepository;
        private readonly IGeneralRepository<User> _userRepository;
        private readonly ICryptographyUtil _cryptographyUtil;
        private readonly AuthTokenOptions _authTokenOptions;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IPasswordUtil _passwordUtil;
        private readonly IUsersService _usersService;
        private readonly IPatientsService _patientsService;
        private readonly IEmployeeService _employeesService;
        private readonly IMapper _mapper;
        private readonly IInputsService _inputsService;
        private readonly IMediator _mediator;
        private readonly IHealthScoreService _healthScoreService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<IAuthService> _logger;

        public AuthService(
            IGeneralRepository<ExternalAuthorization> externalAuthorizationRepository,
            IGeneralRepository<UserIdentity> identityRepository,
            ICryptographyUtil cryptographyUtil,
            IOptions<AuthTokenOptions> authTokenOptions,
            ITokenGenerator tokenGenerator,
            IPasswordUtil passwordUtil,
            IUsersService usersService,
            IPatientsService patientsService,
            IEmployeeService employeesService,
            IMapper mapper,
            IInputsService inputsService,
            IHealthScoreService healthScoreService,
            IWebHostEnvironment environment,
            IMediator mediator,
            ILogger<IAuthService> logger, 
            IGeneralRepository<User> userRepository)
        {
            _externalAuthorizationRepository = externalAuthorizationRepository;
            _identityRepository = identityRepository;
            _cryptographyUtil = cryptographyUtil;
            _authTokenOptions = authTokenOptions.Value;
            _tokenGenerator = tokenGenerator;
            _passwordUtil = passwordUtil;
            _usersService = usersService;
            _patientsService = patientsService;
            _employeesService = employeesService;
            _mapper = mapper;
            _inputsService = inputsService;
            _mediator = mediator;
            _healthScoreService = healthScoreService;
            _environment = environment;
            _logger = logger;
            _userRepository = userRepository;
        }

        /// <summary>
        /// <see cref="IAuthService.GetByIdAsync"/>
        /// </summary>
        public async Task<UserIdentity> GetByIdAsync(int id)
        {
            var identity = await _identityRepository
                .All()
                .ById(id)
                .IncludeUserAndSubUsers()
                .FirstOrDefaultAsync();

            AssertIdentityExist(identity);

            return identity!;
        }

        /// <summary>
        /// <see cref="IAuthService.GetByPatientIdAsync"/>
        /// </summary>
        public async Task<UserIdentity> GetByPatientIdAsync(int id)
        {
            var patient = await _patientsService.GetByIdAsync(id);

            var identity = await _identityRepository
                .All()
                .ById(patient.UserId)
                .IncludeUserAndSubUsers()
                .FirstOrDefaultAsync();

            AssertIdentityExist(identity);

            return identity!;
        }

        /// <summary>
        /// <see cref="IAuthService.GetByEmployeeIdAsync"/>
        /// </summary>
        public async Task<UserIdentity> GetByEmployeeIdAsync(int id)
        {

            var employee = await _employeesService.GetByIdAsync(id);
            
            var identity = await _identityRepository
                .All()
                .ById(employee.UserId)
                .IncludeUserAndSubUsers()
                .FirstOrDefaultAsync();

            AssertIdentityExist(identity);

            return identity!;
        }

        
        /// <summary>
        /// <see cref="IAuthService.GetByEmailAsync"/>
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<UserIdentity> GetByEmailAsync(string email)
        {
            var identity = await _identityRepository
                .All()
                .ByEmail(email)
                .IncludeUserAndSubUsers()
                .Include(i => i.Practice)
                .FirstOrDefaultAsync();

            AssertIdentityExist(identity);

            return identity!;
        }
        
        /// <summary>
        /// <see cref="IAuthService.GetByEmailOrNullAsync"/>
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<UserIdentity> GetByEmailOrNullAsync(string email)
        {
            var identity = await _identityRepository
                .All()
                .ByEmail(email)
                .IncludeUserAndSubUsers()
                .Include(i => i.Practice)
                .FirstOrDefaultAsync();

            return identity!;
        }

        public async Task<Guid> GetUniversalId(int userId)
        {
            return await _userRepository.All()
                .Where(u => u.Id == userId)
                .Select(u => u.UniversalId)
                .FindAsync();
        }

        /// <summary>
        /// Authenticates and returns auth model
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="originPractice"></param>
        /// <param name="onBehalfPractice"></param>
        /// <param name="permissions"></param>
        /// <param name="availableLocationsIds"></param>
        /// <param name="defaultLocationId"></param>
        /// <param name="external"></param>
        /// <returns></returns>
        public async Task<AuthenticationResultModel> Authenticate(UserIdentity identity,
            Practice originPractice,
            Practice? onBehalfPractice,
            PermissionType[] permissions,
            int[] availableLocationsIds,
            int? defaultLocationId,
            ExternalAuthorization? external)
        {
            var roleId = _usersService.GetRoleId(identity);
            var isOnBehalf = onBehalfPractice != null && onBehalfPractice.GetId() != originPractice.GetId();
            var targetLocationId = defaultLocationId ?? availableLocationsIds.First();

            var paymentPlanId = identity.User.Patient?.CurrentSubscription?.PaymentPrice?.PaymentPeriod?.PaymentPlanId;
            var subscriptionId = identity.User.Patient?.CurrentSubscription?.Id;
            var claims = new[]
            {
                new Claim(WhClaims.UserId, identity.GetId().ToString()),
                new Claim(WhClaims.EmployeeId, identity.User.Employee?.Id?.ToString() ?? string.Empty),
                new Claim(WhClaims.PatientId, identity.User.Patient?.Id?.ToString() ?? string.Empty),
                new Claim(WhClaims.OnBehalfPracticeId, onBehalfPractice?.GetId().ToString() ?? string.Empty),
                new Claim(WhClaims.PracticeId, originPractice.GetId().ToString()),
                new Claim(WhClaims.IdentityType, identity.Type.ToString()),
                new Claim(WhClaims.Permissions, string.Join(',', permissions)),
                new Claim(WhClaims.RoleId, roleId?.ToString() ?? string.Empty),
                new Claim(WhClaims.AvailableLocationIds, string.Join(',', availableLocationsIds)),
                new Claim(WhClaims.TargetLocationId, targetLocationId.ToString()),
                new Claim(WhClaims.PaymentPlanId, paymentPlanId?.ToString() ?? string.Empty),
                new Claim(WhClaims.SubscriptionId, subscriptionId.ToString() ?? string.Empty)
            };

            var (toke, expires) = _tokenGenerator.Generate(DateTime.UtcNow, claims);

            var resultModel = _mapper.Map<UserIdentity, AuthenticationResultModel>(identity);

            resultModel.IdentifyPayload = _mapper.Map<IdentifyPayload>(identity.User);

            var patient = identity.User.Patient;

            var subscription = patient?.MostRecentSubscription;

            var orderTypes = patient?.Orders
                .Select(x => x.Type)
                .ToArray() ?? Array.Empty<OrderType>();

            var aggregator = patient != null ? await _inputsService.GetAggregatorAsync(patient.GetId()) : null;

            HealthScoreResponseModel? healthScore = null;

            if (patient != null)
            {
                try
                {
                    healthScore = await _healthScoreService.GetPatientHealthScore(patient.GetId().ToString());
                }
                catch (ClarityCoreException err)
                {
                    _logger.LogInformation($"Unable to get health score for patient id: {patient.GetId()}, error {err.ToString()}");
                }
            }

            var appointmentsSummary = patient != null
                ? await _mediator.Send(new GetAppointmentSummaryCommand(patient.GetId()), CancellationToken.None)
                : null;

            foreach (var itemToMap in new List<object?>
                     {
                         patient,
                         subscription,
                         orderTypes,
                         aggregator,
                         appointmentsSummary,
                         healthScore,
                         _environment
                     }.Where(itemToMap => itemToMap != null))
            {
                _mapper.Map(itemToMap, resultModel.IdentifyPayload);
            }

            // calculated fields not being matched by mapper, assigned explicitly
            resultModel.Token = _cryptographyUtil.Encrypt(toke, _authTokenOptions.Secret);
            resultModel.Expires = expires.ToEpochTime();
            resultModel.IsOnBehalf = isOnBehalf;
            resultModel.RoleId = roleId;
            resultModel.RegistrationCompleted = identity.User.IsRegistrationCompleted;
            resultModel.TargetLocationId = targetLocationId;
            resultModel.Permissions = permissions;
            resultModel.AvailableLocationIds = availableLocationsIds;
            resultModel.PracticeId = isOnBehalf
                ? onBehalfPractice!.GetId()
                : originPractice.GetId();
            resultModel.ExternalAuthentication = external is null
                ? null
                : new ExternalAuthenticationResultModel
                {
                    AccessToken = _cryptographyUtil.Encrypt(external.AccessToken, _authTokenOptions.Secret),
                    Provider = external.Provider.ToString()
                };
            return resultModel;
        }

        /// <summary>
        /// <see cref="IAuthService.CreateAsync"/>
        /// </summary>
        public async Task<UserIdentity> CreateAsync(UserIdentity identity)
        {
            await _identityRepository.AddAsync(identity);

            await _identityRepository.SaveAsync();

            return identity;
        }

        /// <summary>
        /// <see cref="IAuthService.UpdatePassword(UserIdentity,string)"/>
        /// </summary>
        public async Task UpdatePassword(UserIdentity identity, string password)
        {
            AssertIdentityExist(identity);

            if (string.IsNullOrEmpty(password))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Password can not be empty");
            }

            var (passwordHash, passwordSalt) = _passwordUtil.CreatePasswordHash(password);

            identity.PasswordHash = passwordHash;
            identity.PasswordSalt = passwordSalt;

            _identityRepository.Edit(identity);
            await _identityRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="IAuthService.UpdateAsync"/>
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task UpdateAsync(UserIdentity user)
        {
            _identityRepository.Edit(user);

            await _identityRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="IAuthService.UpdatePracticeAsync"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="practice"></param>
        /// <returns></returns>
        public async Task UpdatePracticeAsync(int userId, Practice practice)
        {
            var identity = await _identityRepository.GetAsync(userId);

            AssertIdentityExist(identity);

            identity.PracticeId = practice.GetId();

            _identityRepository.Edit(identity);
            await _identityRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="IAuthService.UpdatePassword(int, string)"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task UpdatePassword(int userId, string password)
        {
            var identity = await _identityRepository.GetAsync(userId);

            await UpdatePassword(identity, password);
        }

        /// <summary>
        /// <see cref="IAuthService.VerifyAsync"/>
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public async Task<UserIdentity> VerifyAsync(UserIdentity identity)
        {
            identity.IsVerified = true;

            _identityRepository.Edit(identity);

            await _identityRepository.SaveAsync();

            return identity;
        }

        /// <summary>
        /// <see cref="IAuthService.DeleteAsync"/>
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            var identity = await _identityRepository.GetAsync(id);

            AssertIdentityIsActive(identity);

            _identityRepository.Delete(identity);

            await _identityRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="IAuthService.CheckIfEmailExistsAsync"/>
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<bool> CheckIfEmailExistsAsync(string email)
        {
            return await _identityRepository
                .All()
                .ByEmail(email)
                .AnyAsync();
        }

        #region external authorization

        /// <summary>
        /// <see cref="IAuthService.GetExternalAuthorizationAsync"/>
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public async Task<ExternalAuthorization> GetExternalAuthorizationAsync(string integrationId,
            AuthorizationProvider provider)
        {
            var authorization = await _externalAuthorizationRepository
                .All()
                .ByIntegrationId(integrationId)
                .ByProvider(provider)
                .FirstOrDefaultAsync();

            if (authorization is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Such external authorization does not exist");
            }

            return authorization;
        }

        /// <summary>
        /// <see cref="IAuthService.GetUserExternalAuthorizationAsync"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ExternalAuthorization> GetUserExternalAuthorizationAsync(int userId)
        {
            var authorization = await _externalAuthorizationRepository
                .All()
                .RelatedToUser(userId)
                .FirstOrDefaultAsync();

            if (authorization is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Such external authorization does not exist");
            }

            return authorization;
        }

        /// <summary>
        /// <see cref="IAuthService.GetExternalAuthorizationByAuthTokenAsync"/>
        /// </summary>
        /// <param name="authToken"></param>
        /// <param name="provider"></param>
        public async Task<ExternalAuthorization> GetExternalAuthorizationByAuthTokenAsync(string authToken,
            AuthorizationProvider provider)
        {
            var authorization = await _externalAuthorizationRepository
                .All()
                .ByAuthToken(authToken)
                .ByProvider(provider)
                .FirstOrDefaultAsync();

            if (authorization is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Such external authorization does not exist");
            }

            return authorization;
        }

        /// <summary>
        /// <see cref="IAuthService.CreateExternalAuthorizationAsync"/>
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        public async Task<ExternalAuthorization> CreateExternalAuthorizationAsync(ExternalAuthorization authorization)
        {
            await _externalAuthorizationRepository.AddAsync(authorization);

            await _externalAuthorizationRepository.SaveAsync();

            return authorization;
        }

        /// <summary>
        /// <see cref="IAuthService.UpdateExternalAuthorizationAsync"/>
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        public async Task<ExternalAuthorization> UpdateExternalAuthorizationAsync(ExternalAuthorization authorization)
        {
            _externalAuthorizationRepository.Edit(authorization);

            await _externalAuthorizationRepository.SaveAsync();

            return authorization;
        }

        #endregion

        #region private

        /// <summary>
        /// Asserts identity exist
        /// </summary>
        /// <param name="identity"></param>
        /// <exception cref="AppException"></exception>
        private void AssertIdentityExist(UserIdentity? identity)
        {
            if (identity is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "User does not exist");
            }
        }

        /// <summary>
        /// Asserts identity is active
        /// </summary>
        /// <param name="identity"></param>
        /// <exception cref="AppException"></exception>
        private void AssertIdentityIsActive(UserIdentity identity)
        {
            if (identity.IsBlocked || identity.IsDeleted())
            {
                throw new AppException(HttpStatusCode.Forbidden, "User is not active");
            }
        }


        public async Task<UserIdentity> GetSpecAsync(int id, ISpecification<UserIdentity> specification)
        {
            var identity = await _identityRepository
                .All()
                .ById(id)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();

            AssertIdentityExist(identity);

            return identity!;
        }

        #endregion
    }
}