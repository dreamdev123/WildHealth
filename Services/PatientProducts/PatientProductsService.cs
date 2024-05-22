using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Products;
using ProductType = WildHealth.Domain.Enums.Products.ProductType;

namespace WildHealth.Application.Services.PatientProducts;

public class PatientProductsService : IPatientProductsService
{
    private readonly IGeneralRepository<PatientProduct> _patientProductsRepository;
    private readonly IGeneralRepository<Patient> _patientsRepository;

    public PatientProductsService(IGeneralRepository<PatientProduct> patientProductsRepository, IGeneralRepository<Patient> patientsRepository)
    {
        _patientProductsRepository = patientProductsRepository;
        _patientsRepository = patientsRepository;
    }
    
    /// <summary>
    /// <see cref="IPatientProductsService.GetAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="AppException"></exception>
    public async Task<PatientProduct> GetAsync(int id)
    {
        var product = await _patientProductsRepository
            .All()
            .ById(id)
            .IncludeIntegrations<PatientProduct, PatientProductIntegration>()
            .IncludeServices()
            .FirstOrDefaultAsync();

        if (product is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "Patient Product does not exist.");
        }

        return product;
    }

    /// <summary>
    /// <see cref="IPatientProductsService.GetActiveAsync"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> GetActiveAsync(int patientId)
    {
        var products = await _patientProductsRepository
            .All()
            .RelatedToPatient(patientId)
            .IsActive()
            .ToArrayAsync();

        return products;
    }

    /// <summary>
    /// <see cref="IPatientProductsService.GetByTypeAsync"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="type"></param>
    /// <param name="builtInSourceId"></param>
    /// <returns></returns>
    public async Task<PatientProduct?> GetByTypeAsync(int patientId, ProductType type, Guid builtInSourceId)
    {
        var products = await _patientProductsRepository
            .All()
            .IsActive()
            .ByAdditionalOrBuiltInSourceId(builtInSourceId)
            .IncludeIntegrations<PatientProduct, PatientProductIntegration>()
            .RelatedToPatient(patientId)
            .ByType(type)
            .ToArrayAsync();

        var builtInProduct = products.FirstOrDefault(x => x.ProductSubType == ProductSubType.BuiltIn);

        return builtInProduct ?? products.FirstOrDefault();
    }

    /// <summary>
    /// <see cref="IPatientProductsService.GetByTypeAsync"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="builtInSourceId"></param>
    /// <returns></returns>
    public async Task<PatientProduct?> GetBySourceIdAndAdditionalAsync(int patientId, Guid builtInSourceId)
    {
        var products = await _patientProductsRepository
            .All()
            .IsActive()
            .ByAdditionalOrBuiltInSourceId(builtInSourceId)
            .IncludeIntegrations<PatientProduct, PatientProductIntegration>()
            .RelatedToPatient(patientId)
            .ToArrayAsync();

        var builtInProduct = products.FirstOrDefault(x => x.ProductSubType == ProductSubType.BuiltIn);

        return builtInProduct ?? products.FirstOrDefault();
    }

    /// <summary>
    /// <see cref="IPatientProductsService.GetByTypeAsync"/>
    /// </summary>
    /// <param name="paymentStatuses"></param>
    /// <param name="paymentFlow"></param>
    /// <param name="usedFrom"></param>
    /// <param name="usedTo"></param>
    /// <param name="patientId"></param>
    /// <param name="productTypes"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> SelectAsync(ProductPaymentStatus[] paymentStatuses,
        PaymentFlow? paymentFlow,
        DateTime? usedFrom,
        DateTime? usedTo,
        int? patientId,
        ProductType[]? productTypes = null)
    {
        return await _patientProductsRepository
            .All()
            .ByPaymentFlow(paymentFlow)
            .ByPaymentStatuses(paymentStatuses)
            .ByProductTypes(productTypes)
            .UsedInTheRange(usedFrom, usedTo)
            .RelatedToPatient(patientId)
            .IncludeIntegrations<PatientProduct, PatientProductIntegration>()
            .ToArrayAsync();
    }

    /// <summary>
    /// Returns all patient products related to this subscription or are purchased additionally outside of a subscription that persist perpetually
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="currentSubscription"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> GetBySubscriptionAsync(int patientId, Subscription currentSubscription)
    {
        return await _patientProductsRepository
            .All()
            .ByCurrentSubscription(patientId, currentSubscription)
            .IncludeIntegrations<PatientProduct, PatientProductIntegration>()
            .ToArrayAsync();
    }

    /// <summary>
    /// Get all by patient and type
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="productType"></param>
    /// <param name="productSubType"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> GetByPatientIdAndProductTypeAsync(int patientId, ProductType productType, ProductSubType productSubType)
    {
        return await _patientProductsRepository
            .All()
            .ByPatientId(patientId)
            .ByType(productType)
            .BySubType(productSubType)
            .IncludeIntegrations<PatientProduct, PatientProductIntegration>()
            .ToArrayAsync();
    }

    /// <summary>
    /// <see cref="IPatientProductsService.CreateAsync(PatientProduct[])"/>
    /// </summary>
    /// <param name="patientProducts"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> CreateAsync(PatientProduct[] patientProducts)
    {
        foreach (var product in patientProducts)
        {
            await _patientProductsRepository.AddAsync(product);
        }

        await _patientProductsRepository.SaveAsync();

        return patientProducts;
    }

    /// <summary>
    /// <see cref="IPatientProductsService.UpdateAsync(PatientProduct[])"/>
    /// </summary>
    /// <param name="patientProducts"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> UpdateAsync(PatientProduct[] patientProducts)
    {
        foreach (var product in patientProducts)
        {
            _patientProductsRepository.Edit(product);
        }

        await _patientProductsRepository.SaveAsync();

        return patientProducts;
    }

    /// <summary>
    /// <see cref="IPatientProductsService.UpdateAsync(PatientProduct)"/>
    /// </summary>
    /// <param name="patientProduct"></param>
    /// <returns></returns>
    public async Task<PatientProduct> UpdateAsync(PatientProduct patientProduct)
    {
        _patientProductsRepository.Edit(patientProduct);

        await _patientProductsRepository.SaveAsync();

        return patientProduct;
    }

    /// <summary>
    /// <see cref="IPatientProductsService.UseAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="usedBy"></param>
    /// <param name="usedAt"></param>
    /// <returns></returns>
    public async Task UseAsync(int id, string usedBy, DateTime usedAt)
    {
        var product = await GetAsync(id);

        product.UseProduct(usedBy, usedAt);

        _patientProductsRepository.Edit(product);

        await _patientProductsRepository.SaveAsync();
    }

    /// <summary>
    /// <see cref="IPatientProductsService.UseBulkAsync"/>
    /// </summary>
    /// <param name="patientProducts"></param>
    /// <param name="usedBy"></param>
    /// <param name="usedAt"></param>
    public async Task UseBulkAsync(IEnumerable<PatientProduct> patientProducts, string usedBy, DateTime usedAt)
    {
        foreach (var patientProduct in patientProducts)
        {
            patientProduct.UseProduct(usedBy, usedAt);
            
            _patientProductsRepository.Edit(patientProduct);
        }

        await _patientProductsRepository.SaveAsync();
    }

    /// <summary>
    /// <see cref="IPatientProductsService.ExpireBulkAsync"/>
    /// </summary>
    /// <param name="patientProducts"></param>
    /// <param name="expiredBy"></param>
    /// <param name="expiredAt"></param>
    /// <returns></returns>
    public async Task ExpireBulkAsync(IEnumerable<PatientProduct> patientProducts, string expiredBy, DateTime expiredAt)
    {
        foreach (var patientProduct in patientProducts)
        {
            patientProduct.ExpireProduct(expiredBy, expiredAt);
            
            _patientProductsRepository.Edit(patientProduct);
        }

        await _patientProductsRepository.SaveAsync();
    }

    /// <summary>
    /// <see cref="IPatientProductsService.GetBuiltInByPatientAsync"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    public async Task<ICollection<PatientProduct>> GetBuiltInByPatientAsync(int patientId)
    {
        return await _patientProductsRepository
            .All()
            .ByPatientId(patientId)
            .IncludeIntegrations<PatientProduct, PatientProductIntegration>()
            .IsActive()
            .IsBuiltIn()
            .ToArrayAsync();
    }

    public async Task<PatientProduct[]> GetBuiltInProductsForCurrentSubscription(int patientId)
    {
        var patient = await _patientsRepository
            .All()
            .Where(o => o.Id == patientId)
            .Include(o => o.Subscriptions)
            .ThenInclude(o => o.Integrations)
            .ThenInclude(o => o.Integration)
            .Include(o => o.Subscriptions)
            .ThenInclude(o => o.PaymentPrice)
            .ThenInclude(o => o.PaymentPeriod)
            .ThenInclude(o => o.PaymentPlan)
            .FirstOrDefaultAsync();

        var activeSubscription = patient?.CurrentSubscription;
        
        return await _patientProductsRepository
            .All()
            .ByCurrentSubscription(patientId, activeSubscription)
            .IncludeIntegrations<PatientProduct, PatientProductIntegration>()
            .IsBuiltIn()
            .ToArrayAsync();
    }

    /// <summary>
    /// <see cref="IPatientProductsService.GetSubscriptionProductsAsync"/>
    /// </summary>
    /// <param name="subscriptionUniversalId"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> GetSubscriptionProductsAsync(Guid subscriptionUniversalId)
    {
        return await _patientProductsRepository.All()
            .Where(x => x.SourceId == subscriptionUniversalId)
            .ToArrayAsync();
    }

    /// <summary>
    /// <see cref="IPatientProductsService.DeleteAsync(PatientProduct)"/>
    /// </summary>
    /// <param name="patientProduct"></param>
    /// <returns></returns>
    public async Task<PatientProduct> DeleteAsync(PatientProduct patientProduct)
    {
        _patientProductsRepository.Delete(patientProduct);

        await _patientProductsRepository.SaveAsync();

        return patientProduct;
    }
    
    /// <summary>
    /// <see cref="IPatientProductsService.DeleteAsync(PatientProduct[])"/>
    /// </summary>
    /// <param name="patientProducts"></param>
    /// <returns></returns>
    public async Task<PatientProduct[]> DeleteAsync(PatientProduct[] patientProducts)
    {
        foreach (var product in patientProducts)
        {
            _patientProductsRepository.Delete(product);
        }

        await _patientProductsRepository.SaveAsync();

        return patientProducts;
    }
}

