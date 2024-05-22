using System.Threading.Tasks;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Products;
using WildHealth.Integration.Models.Invoices;
using WildHealth.Integration.Models.Payments;

namespace WildHealth.Application.Services.ProductsPayment;

/// <summary>
/// Provides methods for working with products payment
/// </summary>
public interface IProductsPaymentService
{
    /// <summary>
    /// Process products payment
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="products"></param>
    /// <param name="patientProducts"></param>
    /// <param name="employerProduct"></param>
    /// <param name="isPaidByDefaultEmployer"></param>
    /// <returns></returns>
    Task<PaymentIntegrationModel> ProcessProductsPaymentAsync(
        Patient patient, 
        (Product product, int quantity)[] products,
        PatientProduct[] patientProducts,
        EmployerProduct employerProduct,
        bool isPaidByDefaultEmployer);

    /// <summary>
    /// Process patient responsibility payment
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="items"></param>
    /// <param name="accountTaxId"></param>
    /// <param name="memo"></param>
    /// <returns></returns>
    Task<InvoiceIntegrationModel> ProcessProductPatientResponsibilityPaymentAsync(
        Patient patient,
        (string productId, decimal price)[] items,
        string accountTaxId,
        string? memo = null);
}