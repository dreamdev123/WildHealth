using System.Threading.Tasks;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Interfaces;
using WildHealth.Domain.Models.Payment;

namespace WildHealth.Application.Utils.PatientProductsFactory;

/// <summary>
/// Represents factory for creating patient products
/// </summary>
public interface IPatientsProductFactory
{
    /// <summary>
    /// Creates and returns build in patient products
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="existing"></param>
    /// <param name="paymentFlow"></param>
    /// <returns></returns>
    public Task<PatientProduct[]> CreateBasedOnExistingAsync(Patient patient, PatientProduct[] existing, PaymentFlow paymentFlow);

    /// <summary>
    /// Creates and returns build in patient products
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="subscription"></param>
    /// <param name="coupon"></param>
    /// <returns></returns>
    public Task<PatientProduct[]> CreateBuildInAsync(Patient patient, Subscription subscription, PaymentCoupon? coupon);
    
    /// <summary>
    /// Creates and returns build in patient products
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="subscription"></param>
    /// <param name="coupon"></param>
    /// <returns></returns>
    public Task<PatientProduct[]> CreateBuildInAsyncV2(Patient patient, Subscription subscription, PromoCodeDomain coupon);
    
    /// <summary>
    /// Creates and returns additional patient products
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="subscription"></param>
    /// <param name="payor"></param>
    /// <param name="products"></param>
    /// <returns></returns>
    public Task<PatientProduct[]> CreateAdditionalAsync(
        Patient patient, 
        Subscription subscription,
        IPayor payor,
        (ProductType type, int quantity)[] products);
}