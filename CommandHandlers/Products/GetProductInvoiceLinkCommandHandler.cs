using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Products;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Enums.Integrations;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Products;

public class GetProductInvoiceLinkCommandHandler : IRequestHandler<GetProductInvoiceLinkCommand, string>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly IPatientsService _patientsService;
    private readonly IPaymentService _paymentService;

    public GetProductInvoiceLinkCommandHandler(
        IPatientProductsService patientProductsService, 
        IPatientsService patientsService,
        IPaymentService paymentService)
    {
        _patientProductsService = patientProductsService;
        _patientsService = patientsService;
        _paymentService = paymentService;
    }

    public async Task<string> Handle(GetProductInvoiceLinkCommand command, CancellationToken cancellationToken)
    {
        var specification = PatientSpecifications.PatientWithIntegrations;

        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);
        
        var patientProduct = await _patientProductsService.GetAsync(command.PatientProductId);
        
        AssertCanGetInvoice(patientProduct);

        var invoiceId = patientProduct.GetInvoiceIntegrationId(
            vendor: IntegrationVendor.Stripe,
            purpose: command.Purpose
        );

        AssertInvoiceIsValid(invoiceId);

        return await _paymentService.GenerateInvoicePageAsync(
            patient: patient,
            invoiceId: invoiceId
        );
    }
    
    #region private

    /// <summary>
    /// Asserts if invoice exists for patient product 
    /// </summary>
    /// <param name="patientProduct"></param>
    /// <exception cref="AppException"></exception>
    private void AssertCanGetInvoice(PatientProduct patientProduct)
    {
        if (patientProduct.PaymentStatus != ProductPaymentStatus.PendingOutstandingInvoice)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Particular product doesn't have invoice");
        }
    }
    
    /// <summary>
    /// Asserts if invoice exists for patient product 
    /// </summary>
    /// <param name="invoiceId"></param>
    /// <exception cref="AppException"></exception>
    private void AssertInvoiceIsValid(string invoiceId)
    {
        if (string.IsNullOrEmpty(invoiceId))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Invoice does not exist");
        }
    }
    
    #endregion
}