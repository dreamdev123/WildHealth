using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PurchasePayorService;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Products;
using WildHealth.Fhir.Models.Claims;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class ProcessFhirClaimUpdatedCommandCommandHandler : IRequestHandler<ProcessFhirClaimUpdatedCommand>
{
    private readonly IAppointmentsService _appointmentsServices;
    private readonly IPatientsService _patientsService;
    private readonly IPatientProductsService _patientProductsService;
    private readonly IPurchasePayorService _purchasePayorService;
    private readonly IInsuranceService _insuranceService;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;

    public ProcessFhirClaimUpdatedCommandCommandHandler(
        IAppointmentsService appointmentsService,
        IPatientsService patientsService,
        IPatientProductsService patientProductsService,
        IPurchasePayorService purchasePayorService,
        IInsuranceService insuranceService,
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory)
    {
        _appointmentsServices = appointmentsService;
        _patientsService = patientsService;
        _patientProductsService = patientProductsService;
        _purchasePayorService = purchasePayorService;
        _insuranceService = insuranceService;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
    }

    public async Task Handle(ProcessFhirClaimUpdatedCommand command, CancellationToken cancellationToken)
    {
        var patient = await GetPatient(command.PatientIntegrationId);
        var appointment = await GetAppointment(command.AppointmentIntegrationId);

        var patientProduct = appointment.PatientProduct;
        var patientBalance = command.PatientBalance;
        var insuranceBalance = command.InsuranceBalance;
        var claimIntegrationId = command.ClaimIntegrationId;
        var insuranceIntegrationId = command.InsuranceIntegrationId;
        var practiceId = patient.User.PracticeId;

        if (patientBalance > 0)
        {
            patientProduct.MarkAsPendingOutstandingInvoice();
        } else if (insuranceBalance > 0)
        {
            patientProduct.MarkAsPendingInsuranceClaim();
        }
        else
        {
            var claim = await GetClaimAsync(practiceId, claimIntegrationId);

            var claimStatus = GetExtension(claim, OpenPmConstants.Claim.Extension.Status);

            if (claimStatus == OpenPmConstants.Claim.ClaimStatus.FinalizedPaid && patientProduct.PaymentStatus != ProductPaymentStatus.Paid)
            {
                patientProduct.MarkAsPaid(claimIntegrationId);
                await CreatePurchasePayorEntries(
                    patient,
                    patientProduct,
                    practiceId,
                    claimIntegrationId,
                    insuranceIntegrationId
                );
            }
            else
            {
                patientProduct.MarkAsPendingInsuranceClaim();
            }
        }

        await _patientProductsService.UpdateAsync(new[] { patientProduct });
    }

    #region private

    private async Task<Appointment> GetAppointment(string integrationId)
    {
        var appointment = await _appointmentsServices.GetByIntegrationIdAsync(
            integrationId,
            IntegrationVendor.OpenPm,
            IntegrationPurposes.Appointment.ExternalId);

        if (appointment is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, $"OpenPm Appointment with id {integrationId} is not linked.");
        }

        return appointment;
    }
    
    private async Task<Patient> GetPatient(string integrationId)
    {
        var patient = await _patientsService.GetByIntegrationIdAsync(
            integrationId,
            IntegrationVendor.OpenPm);

        if (patient is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, $"OpenPm Patient with id {integrationId} is not linked.");
        }

        return patient;
    }

    private async Task<ClaimModel> GetClaimAsync(int practiceId, string claimId)
    {
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(practiceId);
        
        return await pmService.GetClaimAsync(practiceId, claimId);
    }

    private string GetExtension(ClaimModel claimModel, string id)
    {
         var status = claimModel
             .Extensions
             .FirstOrDefault()
             ?.Extensions
             .FirstOrDefault(x => x.Id == id)
             ?.Value
             .Codings
             .FirstOrDefault()
             ?.Code;
    
         if (status is null)
         {
             throw new AppException(HttpStatusCode.BadRequest, $"No {id} field on the claim");
         }
    
         return status;
     }

    private async Task CreatePurchasePayorEntries(
        Patient patient,
        PatientProduct patientProduct,
        int practiceId,
        string claimIntegrationId,
        string insuranceIntegrationId)
    {
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(practiceId);
        
        var paymentRecs = await pmService.QueryPaymentRecsAsync(practiceId, claimIntegrationId);

        var patientPaid = Decimal.Zero;
        var insurancePaid = Decimal.Zero;
        
        foreach (var paymentRec in paymentRecs)
        {
            var details = paymentRec.Details;

            foreach (var detail in details)
            {
                if (detail.Claim.Identifier != $"Claim/{claimIntegrationId}")
                {
                    continue;
                }
                
                var type = detail.Type?.Codings.FirstOrDefault()?.Code;

                switch(type)
                {
                    case OpenPmConstants.PaymentRec.Type.InsurancePayment : 
                        insurancePaid += detail.Amount.Value;
                        break;
                    case OpenPmConstants.PaymentRec.Type.PatientPayment:
                        patientPaid += detail.Amount.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        var insurance = await _insuranceService.GetByIntegrationIdAsync(insuranceIntegrationId, IntegrationVendor.OpenPm);

        await _purchasePayorService.CreateAsync(
            payable: patientProduct,
            payor: patient,
            patient: patient,
            amount: patientPaid,
            billableOnDate: DateTime.UtcNow
        );
        
        await _purchasePayorService.CreateAsync(
            payable: patientProduct,
            payor: insurance,
            patient: patient,
            amount: insurancePaid,
            billableOnDate: DateTime.UtcNow
        );
    }

    #endregion
}