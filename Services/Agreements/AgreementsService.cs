using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Utils.AgreementFactory;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Models.Agreements;
using WildHealth.Domain.Entities.Agreements;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Common.Extensions;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Application.Services.Attachments;
using WildHealth.Domain.Entities.Attachments;

namespace WildHealth.Application.Services.Agreements
{
    /// <summary>
    /// <see cref="IAgreementsService"/>
    /// </summary>
    public class AgreementsService : IAgreementsService
    {
        private readonly IAgreementFactory _agreementFactory;
        private readonly IGeneralRepository<Patient> _patientsRepository;
        private readonly IGeneralRepository<Agreement> _agreementsRepository;
        private readonly IGeneralRepository<AgreementConfirmation> _agreementConfirmationsRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IAttachmentsService _attachmentsService;
        
        public AgreementsService(
            IAgreementFactory agreementFactory, 
            IGeneralRepository<Patient> patientsRepository,
            IGeneralRepository<Agreement> agreementsRepository,
            IGeneralRepository<AgreementConfirmation> agreementConfirmationsRepository,
            IHttpContextAccessor httpContextAccessor,
            IAzureBlobService azureBlobService,
            IAttachmentsService attachmentsService)
        {
            _agreementFactory = agreementFactory;
            _patientsRepository = patientsRepository;
            _agreementsRepository = agreementsRepository;
            _agreementConfirmationsRepository = agreementConfirmationsRepository;
            _httpContextAccessor = httpContextAccessor;
            _azureBlobService = azureBlobService;
            _attachmentsService = attachmentsService;
        }

        /// <summary>
        /// <see cref="IAgreementsService.GetUnsignedConfirmationsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AgreementConfirmation>> GetUnsignedConfirmationsAsync(int patientId)
        {
            var confirmations = await _agreementConfirmationsRepository
                .All()
                .RelatedToPatient(patientId)
                .Unsigned()
                .IncludeAgreements()
                .IncludeSubscription()
                .AsNoTracking()
                .ToArrayAsync();

            return confirmations;
        }

        /// <summary>
        /// <see cref="IAgreementsService.GetChangedAgreementsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Agreement>> GetChangedAgreementsAsync(int patientId)
        {
            var confirmations = await _agreementConfirmationsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeAgreements()
                .Changed()
                .ToArrayAsync();

            var agreements = confirmations.Select(x => x.Agreement).Distinct().ToArray();

            return agreements;
        }

        /// <summary>
        /// <see cref="IAgreementsService.GetPatientConfirmationWithDocumentAsync(Guid, int)"/>
        /// </summary>
        /// <param name="intakeId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<AgreementConfirmation> GetPatientConfirmationWithDocumentAsync(Guid intakeId, int id)
        {
            var confirmation = await _agreementConfirmationsRepository
                .All()
                .Signed()
                .RelatedToPatient(intakeId)
                .IncludeAgreements()
                .IncludeDocument()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (confirmation is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Agreement confirmation does not exist");
            }

            return confirmation;
        }

        /// <summary>
        /// for remove
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AgreementConfirmation>> GetAgreementConfirmationRange(int from, int to)
        {
            return await _agreementConfirmationsRepository
                .All()
                .Where(x=> x.Id > from && x.Id <= to)
                .ToListAsync();
        }

        /// <summary>
        /// <see cref="IAgreementsService.GetPatientConfirmationWithDocumentAsync(int, int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<AgreementConfirmation> GetPatientConfirmationWithDocumentAsync(int patientId, int id)
        {
            var confirmation = await _agreementConfirmationsRepository
                .All()
                .Signed()
                .RelatedToPatient(patientId)
                .ById(id)
                .IncludeAgreements()
                .IncludeDocument()
                .IncludeAgreementDocument()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (confirmation is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Agreement confirmation does not exist.");
            }
            
            return confirmation;
        }

        /// <summary>
        /// <see cref="IAgreementsService.GetPatientConfirmationsAsync(Guid)"/>
        /// </summary>
        /// <param name="intakeId"></param>
        /// <returns></returns>
        public async Task<AgreementConfirmation[]> GetPatientConfirmationsAsync(Guid intakeId)
        {
            var patient = await _patientsRepository
                .All()
                .AsNoTracking()
                .Where(x => x.IntakeId == intakeId)
                .IncludeAgreements()
                .FirstOrDefaultAsync();
            
            return patient is null 
                ? Array.Empty<AgreementConfirmation>() 
                : patient.Agreements.Where(x => x.IsSigned).ToArray();
        }

        /// <summary>
        /// <see cref="IAgreementsService.GetPatientConfirmationsAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<AgreementConfirmation[]> GetPatientConfirmationsAsync(int patientId)
        {
            var confirmations = await _agreementConfirmationsRepository
                .All()
                .RelatedToPatient(patientId)
                .Signed()
                .IncludeAgreements()
                .IncludeDocument()
                .ToArrayAsync();

            return confirmations;
        }

        /// <summary>
        /// <see cref="IAgreementsService.CreateUnsignedConfirmationsAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AgreementConfirmation>> CreateUnsignedConfirmationsAsync(Patient patient, Subscription subscription)
        {
            var paymentPrice = subscription.PaymentPrice;
            
            var agreements = await FetchCorrespondingAgreementsAsync(paymentPrice.PaymentPeriodId);

            var agreementConfirmations = new List<AgreementConfirmation>();
            
            foreach (var agreement in agreements)
            {
                var agreementConfirmation = new AgreementConfirmation(
                    patient: patient,
                    agreement: agreement,
                    subscription: subscription);

                await _agreementConfirmationsRepository.AddAsync(agreementConfirmation);
                agreementConfirmations.Add(agreementConfirmation);
            }

            await _agreementConfirmationsRepository.SaveAsync();

            return agreementConfirmations;
        }

        /// <summary>
        /// <see cref="IAgreementsService.ConfirmAgreementsAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="subscription"></param>
        /// <param name="models"></param>
        /// <returns></returns>
        public async Task<AgreementConfirmation[]> ConfirmAgreementsAsync(Patient patient,
            Subscription subscription,
            ConfirmAgreementModel[]? models)
        {
            if (models is null || models.Any(x => !x.IsConfirmed))
            {
                throw new AppException(HttpStatusCode.BadRequest, "All agreements should be confirmed");
            }

            var paymentPrice = subscription.PaymentPrice;
            var paymentPeriod = paymentPrice.PaymentPeriod;
            var paymentPlan = paymentPeriod.PaymentPlan;
            
            var agreements = await FetchCorrespondingAgreementsAsync(paymentPrice.PaymentPeriodId);
            if (models.Count() != agreements.Count())
            {
                throw new AppException(HttpStatusCode.BadRequest, "Not all agreements confirmed");
            }

            var ipAddress = GetIpAddress();

            var agreementConfirmations = new List<AgreementConfirmation>(models.Count());
            
            foreach (var agreement in agreements)
            {
                var model = models.FirstOrDefault(x => x.AgreementId == agreement.Id);
                
                if (model is null)
                {
                    var exceptionParam = new AppException.ExceptionParameter(nameof(agreement.Id), agreement.Id);
                    throw new AppException(HttpStatusCode.BadRequest, "Agreement is not confirmed", exceptionParam);
                }

                if (patient == null)
                {
                    continue;
                }
                
                var signedDocument = await _agreementFactory.CreateAsync(
                    agreement: agreement,
                    patient: patient,
                    ipAddress: ipAddress!,
                    planName: paymentPlan.Name,
                    periodInMonth: paymentPeriod.PeriodInMonths,
                    paymentStrategy: paymentPrice.Strategy
                );

                var fileName = GenerateFileName(
                    patientId: patient.GetId(), 
                    agreementName: agreement.Name
                );

                var blobUri = await _azureBlobService.CreateUpdateBlobBytes(
                    containerName: AzureBlobContainers.Attachments,
                    blobName: fileName,
                    fileBytes: signedDocument
                );

                var attachment = await _attachmentsService.CreateOrUpdateWithBlobAsync(
                    attachmentName: fileName,
                    description: "Confirmed agreement",
                    attachmentType: AttachmentType.AgreementConfirmationDocument,
                    path: blobUri,
                    referenceId: patient.GetId()
                );

                var agreementConfirmation = new AgreementConfirmation(
                    patient: patient,
                    agreement: agreement,
                    subscription: subscription,
                    attachment: attachment,
                    isAgree: model.IsConfirmed,
                    ipAddress: ipAddress
                );

                await _agreementsRepository.AddRelatedEntity(agreementConfirmation);
                
                agreementConfirmations.Add(agreementConfirmation);
            }

            await _agreementsRepository.SaveAsync();

            return agreementConfirmations.ToArray();
        }

        /// <summary>
        /// <see cref="IAgreementsService.SignAgreementAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="confirmation"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<AgreementConfirmation> SignAgreementAsync(
            Patient patient, 
            AgreementConfirmation confirmation, 
            ConfirmAgreementModel model)
        {
            var agreement = confirmation.Agreement;
            var subscription = confirmation.Subscription;
            var ipAddress = GetIpAddress();
            var paymentPrice = subscription.PaymentPrice;
            var paymentPeriod = paymentPrice.PaymentPeriod;
            var paymentPlan = paymentPeriod.PaymentPlan;
            
            var signedDocument = await _agreementFactory.CreateAsync(
                agreement: agreement, 
                patient: patient,
                ipAddress: ipAddress!,
                planName: paymentPlan.Name,
                periodInMonth: paymentPeriod.PeriodInMonths,
                paymentStrategy: paymentPrice.Strategy
            );

            var fileName = GenerateFileName(
                patientId: patient.GetId(), 
                agreementName: agreement.Name
            );
            
            var blobUri = await _azureBlobService.CreateUpdateBlobBytes(
                containerName: AzureBlobContainers.Attachments,
                blobName: fileName,
                fileBytes: signedDocument
            );

            var attachment = await _attachmentsService.CreateOrUpdateWithBlobAsync(
                attachmentName: fileName,
                description: "Signed agreement",
                attachmentType: AttachmentType.AgreementConfirmationDocument,
                path: blobUri,
                referenceId: patient.GetId()
            );

            confirmation.Sign(
                isAgree: model.IsConfirmed,
                ipAddress: ipAddress,
                attachment: attachment
            );
            
            return await UpdateAgreementConfirmationsAsync(confirmation);
        }

        /// <summary>
        /// <see cref="IAgreementsService.CopyAgreementsAsync(Patient, Subscription, Subscription)"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="oldSubscription"></param>
        /// <param name="newSubscription"></param>
        /// <returns></returns>
        public async Task<AgreementConfirmation[]> CopyAgreementsAsync(Patient patient, Subscription oldSubscription, Subscription newSubscription)
        {
            var patientsAgreements = await GetPatientConfirmationsAsync(patient.GetId());

            var oldAgreements = patientsAgreements
                .Where(c => c.SubscriptionId == oldSubscription.GetId())
                .ToArray();

            var newAgreements = oldAgreements.Select(c =>
            {
                var oldAttachment = c.Attachment.Attachment;
                var attachment = new Attachment(
                    description: oldAttachment.Description,
                    referenceId: oldAttachment.ReferenceId,
                    type: oldAttachment.Type,
                    name: oldAttachment.Name,
                    path: oldAttachment.Path
                );
                
                var confirmation = new AgreementConfirmation(
                    patient: patient,
                    agreement: c.Agreement,
                    subscription: newSubscription,
                    attachment: attachment,
                    isAgree: c.IsAgree,
                    ipAddress: c.IpAddress
                );

                return confirmation;
            }).ToArray();

            foreach (var newAgreement in newAgreements)
            {
                await _agreementConfirmationsRepository.AddAsync(newAgreement);
            }
            
            await _agreementConfirmationsRepository.SaveAsync();

            return newAgreements;
        }
        
        public string GenerateFileName(int patientId, string agreementName)
        {
            return $"{patientId}/{agreementName}_{DateTime.UtcNow.ToEpochTime()}_{Guid.NewGuid().ToString()[..8]}.pdf";
        }

        #region private
        
        private async Task<AgreementConfirmation> UpdateAgreementConfirmationsAsync(AgreementConfirmation agreementConfirmation)
        {
            _agreementConfirmationsRepository.Edit(agreementConfirmation);

            await _agreementConfirmationsRepository.SaveAsync();

            return agreementConfirmation;
        }
        
        /// <summary>
        /// Fetches and returns corresponding agreements
        /// </summary>
        /// <param name="periodId"></param>
        /// <returns></returns>
        private async Task<Agreement[]> FetchCorrespondingAgreementsAsync(int periodId)
        {
            var agreements = await _agreementsRepository
                .All()
                .Active()
                .IncludePaymentPlans()
                .ToArrayAsync();

            return agreements
                    .Where(x => x.PaymentPlanAgreements.Any(t => t.PaymentPeriodId == periodId))
                    .ToArray();
        }

        /// <summary>
        /// Returns IP address
        /// </summary>
        /// <returns></returns>
        private string? GetIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        #endregion
    }
}