using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Utils.ApplyEmployerUtil;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Domain.Enums.User;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;
using WildHealth.Common.Models.AddOns;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Orders;

namespace WildHealth.Application.Services.AddOns
{
    /// <summary>
    /// <see cref="IAddOnsService"/>
    /// </summary>
    public class AddOnsService : IAddOnsService
    {
        private readonly IGeneralRepository<PaymentPlanAddOn> _paymentPlanAddOnsRepository;
        private readonly IGeneralRepository<AddOn> _addOnsRepository;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IEmployerProductService _employerProductService;
        private readonly IEmployerProductDiscountUtil _employerProductDiscountUtil;
        private readonly ILogger<AddOnsService> _logger;

        public AddOnsService(
            IGeneralRepository<PaymentPlanAddOn> paymentPlanAddOnsRepository,
            IGeneralRepository<AddOn> addOnsRepository,
            IFeatureFlagsService featureFlagsService,
            IEmployerProductService employerProductService,
            IEmployerProductDiscountUtil employerProductDiscountUtil,
            ILogger<AddOnsService> logger
            )
        {
            _paymentPlanAddOnsRepository = paymentPlanAddOnsRepository;
            _featureFlagsService = featureFlagsService;
            _addOnsRepository = addOnsRepository;
            _employerProductService = employerProductService;
            _employerProductDiscountUtil = employerProductDiscountUtil;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IAddOnsService.GetAvailableForOrderingAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="gender"></param>
        /// <param name="orderType"></param>
        /// <param name="employerKey"></param>
        /// <param name="grouping"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AddOn>> GetAvailableForOrderingAsync(
            int practiceId,
            Gender gender,
            OrderType? orderType = null,
            string? employerKey=null,
            bool grouping = false)
        {
            var addOns = await _addOnsRepository
                .All()
                .Active()
                .RelatedToPractice(practiceId)
                .SpecificForGender(gender)
                .ByOrderType(orderType)
                .AvailableForOrdering()
                .IncludeChildren()
                .IncludeAlternative()
                .AsNoTracking()
                .ToArrayAsync();

            var employerProduct = await _employerProductService.GetByKeyAsync(employerKey);

            _employerProductDiscountUtil.ApplyDiscount(addOns, employerProduct);

            if (grouping)
            {
                var children = addOns
                    .Where(x => x.Children.Any())
                    .SelectMany(x => x.Children.Select(k => k.Child))
                    .ToArray();

                return addOns
                    .Where(x => children.All(a => a.Id != x.Id))
                    .Distinct()
                    .ToArray();
            }
            else
            {
                return addOns
                    .SelectMany(x => x.IsGroup
                        ? x.Children.Select(k => k.Child)
                        : new[] { x })
                    .Distinct()
                    .ToArray();
            }
        }

        /// <summary>
        /// <see cref="IAddOnsService.SelectAvailableForOrderingAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="gender"></param>
        /// <param name="provider"></param>
        /// <param name="employerKey"></param>
        /// <param name="searchQuery"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<(IEnumerable<AddOn>, int)> SelectAvailableForOrderingAsync(int practiceId,
            Gender gender,
            AddOnProvider provider,
            string? employerKey,
            string? searchQuery,
            int? skip = 0,
            int? take = 50)
        {
            var queryData = _addOnsRepository
                .All()
                .Active()
                .OrderBy(x => x.Id)
                .RelatedToPractice(practiceId)
                .SpecificForGender(gender)
                .AvailableForOrdering()
                .ByProvider(provider)
                .BySearchQuery(searchQuery)
                .IncludeChildren()
                .IncludeAlternative();
            
            var totalCount = await queryData.CountAsync();
            
            var addOns = await queryData.Pagination(skip, take).ToArrayAsync();

            var employerProduct = await _employerProductService.GetByKeyAsync(employerKey);

            _employerProductDiscountUtil.ApplyDiscount(addOns, employerProduct);
            
            var useDefaultFlow = !_featureFlagsService.GetFeatureFlag(Common.Constants.FeatureFlags.LabOrdersChangeHealthCare);

            if (useDefaultFlow)
            {
                return (addOns, totalCount);
            }

            var filteredAddOns = addOns
                .SelectMany(x => x.IsGroup
                    ? x.Children.Select(k => k.Child)
                    : new[] { x })
                .Distinct()
                .ToArray();

            return (filteredAddOns, totalCount);
        }

        /// <summary>
        /// <see cref="IAddOnsService.GetOptionalAsync"/>
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <param name="gender"></param>
        /// <param name="employerKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AddOn>> GetOptionalAsync(int paymentPlanId, Gender gender, string? employerKey)
        {
            var addOns = await _paymentPlanAddOnsRepository
                .All()
                .IncludeAll()
                .RelatedToPaymentPlan(paymentPlanId: paymentPlanId, required: false)
                .SelectAddons()
                .SpecificForGender(gender)
                .Active()
                .ToArrayAsync();

            var employerProduct = await _employerProductService.GetByKeyAsync(employerKey);

            _employerProductDiscountUtil.ApplyDiscount(addOns, employerProduct);
           
            return addOns;
        }

        /// <summary>
        /// <see cref="IAddOnsService.GetRequiredAsync"/>
        /// </summary>
        /// <param name="paymentPlanId"></param>
        /// <param name="gender"></param>
        /// <param name="employerKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AddOn>> GetRequiredAsync(int paymentPlanId, Gender gender, string? employerKey)
        {
            var addOns = await _paymentPlanAddOnsRepository
                .All()
                .IncludeAll()
                .RelatedToPaymentPlan(paymentPlanId: paymentPlanId, required: true)
                .SelectAddons()
                .SpecificForGender(gender)
                .Active()
                .ToArrayAsync();

            var employerProduct = await _employerProductService.GetByKeyAsync(employerKey);

            _employerProductDiscountUtil.ApplyDiscount(addOns, employerProduct);
            
            return addOns;
        }

        /// <summary>
        /// <see cref="IAddOnsService.GetByIntegrationIdsAsync"/>
        /// </summary>
        /// <param name="integrationIds"></param>
        /// <param name="employerKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AddOn>> GetByIntegrationIdsAsync(string[] integrationIds, string? employerKey)
        {
            var addOns = await _addOnsRepository
                .All()
                .Active()
                .ByIntegrationIds(integrationIds)
                .IncludeChildren()
                .IncludeAlternative()
                .ToArrayAsync();

            var employerProduct = await _employerProductService.GetByKeyAsync(employerKey);

            _employerProductDiscountUtil.ApplyDiscount(addOns, employerProduct);
            
            if (addOns.Count() == integrationIds.Count())
            {
                return addOns;
            }

            var missingIntegrationId = integrationIds.FirstOrDefault(x => addOns.All(k => k.IntegrationId != x));

            var exceptionParam =
                new AppException.ExceptionParameter(nameof(missingIntegrationId), missingIntegrationId);
            throw new AppException(HttpStatusCode.NotFound, "Add-on does not exist.", exceptionParam);
        }

        /// <summary>
        /// <see cref="IAddOnsService.GetByIdsAsync"/>
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="practiceId"></param>
        /// <param name="employerKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AddOn>> GetByIdsAsync(IEnumerable<int> ids, int practiceId, string? employerKey = default)
        {
            var addOnIds = ids.ToList();
            var addOns = await _addOnsRepository
                .All()
                .Active()
                .ByIds(addOnIds.ToArray())
                .RelatedToPractice(practiceId)
                .IncludeChildren()
                .IncludeAlternative()
                .ToListAsync();

            var employerProduct = await _employerProductService.GetByKeyAsync(employerKey);
            _employerProductDiscountUtil.ApplyDiscount(addOns, employerProduct);

            if (addOnIds.Count == addOns.Count)
            {
                return addOns;
            }

            var missingAddOnId = addOnIds.First(x => addOns.All(k => k.Id != x));
            var exceptionParam = new AppException.ExceptionParameter(nameof(missingAddOnId), missingAddOnId);
            throw new AppException(HttpStatusCode.NotFound, "Add-On does not exist.", exceptionParam);
        }

        /// <summary>
        /// <see cref="IAddOnsService.CreateAddOnAsync"/>
        /// </summary>
        /// <param name="addOn"></param>
        /// <returns></returns>
        public async Task<AddOn> CreateAddOnAsync(AddOn addOn)
        {
            await _addOnsRepository.AddAsync(addOn);

            await _addOnsRepository.SaveAsync();

            return addOn;
        }

        /// <summary>
        /// <see cref="IAddOnsService.GetByProviderAsync"/>
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, AddOn>> GetByProviderAsync(AddOnProvider provider)
        {
            var addOns = await _addOnsRepository
                .All()
                .Active()
                .Where(x => x.Provider == provider && !string.IsNullOrEmpty(x.IntegrationId))
                .ToArrayAsync();
            
            var addOnsResult = new Dictionary<string, AddOn>();
            if (!addOns.Any())
            {
                return addOnsResult;
            }
            foreach (var group in addOns.GroupBy(x => x.IntegrationId))
            {
                addOnsResult.Add(group.Key, group.First());
            }
            return addOnsResult;
        }

        /// <summary>
        /// <see cref="IAddOnsService.UpdateAsync"/>
        /// </summary>
        /// <param name="addOn"></param>
        public async Task UpdateAsync(AddOn addOn)
        {
            _addOnsRepository.Edit(addOn);
            await _addOnsRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="IAddOnsService.GetByIdAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<AddOn> GetByIdAsync(int id)
        {
            var addOn = await _addOnsRepository
                .All()
                .Active()
                .ById(id)
                .IncludeChildren()
                .IncludeAlternative()
                .FirstOrDefaultAsync();

            if (addOn is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, $"Unable to find active addOn for [AddOnId] = {id}", exceptionParam);
            }

            return addOn;
        }

        /// <summary>
        /// <see cref="IAddOnsService.EditAddOnAsync"/>
        /// </summary>
        /// <param name="addOnModel"></param>
        /// <returns></returns>
        public async Task<AddOn> EditAsync(AddOnModel addOnModel)
        {
            var addOn = await GetByIdAsync(addOnModel.Id);
            
            if (addOn is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(addOnModel.Id), addOnModel.Id);
                throw new AppException(HttpStatusCode.NotFound, "Add-on does not exist", exceptionParam);
            }
               
            addOn.Name = addOnModel.Name;
            addOn.Description = addOnModel.Name;
            addOn.Instructions = addOnModel.Instructions;
            addOn.IntegrationId = addOnModel.IntegrationId;
            addOn.Price = addOnModel.Price;
            addOn.Provider = addOnModel.Provider;
            addOn.OrderType = addOnModel.OrderType;

            _addOnsRepository.Edit(addOn);

            await _addOnsRepository.SaveAsync();

            return addOn;
        }

        /// <summary>
        /// <see cref="IAddOnsService.DeleteAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<AddOn> DeleteAsync(int id)
        {
            var addOn = await GetByIdAsync(id);
            
            if (addOn is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Add-on does not exist", exceptionParam);
            }

            try
            {
                _addOnsRepository.Delete(addOn);

                await _addOnsRepository.SaveAsync();
            }
            catch 
            {
                // Here is potential issue with existing references
                // In order to not to use cascade delete - use soft delete
                addOn.IsActive = false;
                
                _addOnsRepository.Edit(addOn);
                
                await _addOnsRepository.SaveAsync();
            }

            return addOn;
        }

        /// <summary>
        /// Takes in an AddOn and locates the parent addon for the given patient, if the provided addOn is a parent, it returns the addOn
        /// </summary>
        /// <param name="addOn"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<AddOn?> GetParentForAddOn(AddOn addOn, Patient patient)
        {
            _logger.LogInformation($"Getting addOn parent for [addOnId] = {addOn.GetId()}, [Name] = {addOn.Name}");
            
            // If a single item is passed in and it's 
            if (addOn.IsGroup)
            {
                return addOn;
            }
            
            var practiceId = patient.User.PracticeId;

            _logger.LogInformation($"Searching for parent for [addOnId] = {addOn.GetId()}, [Name] = {addOn.Name}, [PracticeId] = {practiceId}");

            var parents = await _addOnsRepository
                .All()
                .Where(o => o.Id == addOn.GetId())
                .SelectMany(o => o.Parents)
                .Select(o => o.Parent)
                .Where(o =>
                    o.IsActive && o.PracticeId == practiceId && o.Gender == patient.User.Gender)
                .ToArrayAsync();
            
            // If we cannot find any parents, just return the AddOn
            if (parents.IsNullOrEmpty())
            {
                _logger.LogInformation($"Could not find parent for [addOnId] = {addOn.GetId()}, [Name] = {addOn.Name}, [PracticeId] = {practiceId}");

                return addOn;
            }

            // Find the parent that has the most entries, should just be one
            var parent = parents.GroupBy(o => o).MaxBy(o => o.Count());
            
            _logger.LogInformation($"Found parent for [addOnId] = {addOn.GetId()}, [Name] = {addOn.Name}, [PracticeId] = {practiceId}, [ParentAddOnId] = {parent?.Key.GetId()}, [ParentAddOnName] = {parent?.Key.Name}");
            
            return parent?.Key;
        }
    }
}