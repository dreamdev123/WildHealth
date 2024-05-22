using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Extensions;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Shared.Data.Queries;

namespace WildHealth.Application.Services.Subscriptions
{
    /// <summary>
    /// <see cref="ISubscriptionService"/>
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IGeneralRepository<SubscriptionCancellationRequest> _subscriptionCancellationRequestsRepository;
        private readonly IGeneralRepository<Subscription> _subscriptionRepository;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IGeneralRepository<SubscriptionCancellationRequest> subscriptionCancellationRequestsRepository,
            IGeneralRepository<Subscription> subscriptionRepository,
            ILogger<SubscriptionService> logger)
        {
            _subscriptionCancellationRequestsRepository = subscriptionCancellationRequestsRepository;
            _subscriptionRepository = subscriptionRepository;
            _logger = logger;
        }
        
        /// <summary>
        /// <see cref="ISubscriptionService.CreatePastSubscriptionAsync(Patient, PaymentPrice, DateTime, DateTime)"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="paymentPrice"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public async Task<Subscription> CreatePastSubscriptionAsync(
            Patient patient,
            PaymentPrice paymentPrice,
            DateTime startDate,
            DateTime endDate)
        {
            _logger.LogInformation($"Creating of past subscription for patient with [Id] = {patient.Id} started.");

            if (startDate.Date > endDate.Date)
            {
                _logger.LogError($"Creating of past subscription for patient with [Id] = {patient.Id} failed.");
                throw new AppException(HttpStatusCode.BadRequest, "Can't create past subscription with end date greater then current date");
            }

            var subscription = new Subscription(
                paymentPrice: paymentPrice,
                patient: patient,
                startDate: startDate,
                endDate: endDate);

            await _subscriptionRepository.AddAsync(subscription);
            await _subscriptionRepository.SaveAsync();

            _logger.LogInformation($"Creating of past subscription for patient with [Id] = {patient.Id} finished.");

            return subscription;
        }

        /// <summary>
        /// <see cref="ISubscriptionService.UpdateSubscriptionAsync"/>
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public async Task<Subscription> UpdateSubscriptionAsync(Subscription subscription)
        {
            _subscriptionRepository.Edit(subscription);

            await _subscriptionRepository.SaveAsync();

            return subscription;
        }

        /// <summary>
        /// <see cref="ISubscriptionService.ScheduleCancellationAsync"/>
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="cancellationType"></param>
        /// <param name="cancellationReason"></param>
        /// <param name="cancellationDate"></param>
        /// <returns></returns>
        public async Task<Subscription> ScheduleCancellationAsync(
            Subscription subscription, 
            CancellationReasonType cancellationType,
            string cancellationReason, 
            DateTime cancellationDate)
        {
            if (subscription.CancellationRequest is null)
            {
                var cancellationRequest = new SubscriptionCancellationRequest(
                    subscription: subscription,
                    reasonType: cancellationType,
                    reason: cancellationReason,
                    date: cancellationDate
                );

                await _subscriptionCancellationRequestsRepository.AddAsync(cancellationRequest);

                await _subscriptionCancellationRequestsRepository.SaveAsync();
            }
            else 
            {
                subscription.CancellationRequest.Reschedule(
                    reasonType: cancellationType,
                    reason: cancellationReason,
                    date: cancellationDate
                );

                _subscriptionRepository.Edit(subscription);

                await _subscriptionRepository.SaveAsync();
            }
            
            return subscription;
        }

        /// <summary>
        /// <see cref="ISubscriptionService.GetCurrentSubscriptionAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<Subscription> GetCurrentSubscriptionAsync(int patientId)
        {
            var subscription = await _subscriptionRepository
                .All()
                .OrderBy(x => x.EndDate)
                .CurrentSubscription(patientId)
                .IncludePaymentPlan()
                .IncludeEmployerProduct()
                .IncludePauses()
                .IncludeIntegrations<Subscription, SubscriptionIntegration>()
                .IncludePaymentPlanIntegration()
                .IncludeCancellationRequest()
                .IncludePromoCode()
                .FirstOrDefaultAsync();

            if (subscription is null)
            {
                _logger.LogWarning($"Subscription for patient with [Id] = {patientId} does not exist.");
                throw new AppException(HttpStatusCode.NotFound, $"Subscription for patient with id: {patientId} does not exist");
            }

            return subscription;
        }

        /// <summary>
        /// <see cref="ISubscriptionService.GetAllAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<Subscription[]> GetAllAsync(int patientId)
        {
            var subscriptions = await _subscriptionRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeCancellationRequest()
                .IncludePauses()
                .IncludeIntegrations<Subscription, SubscriptionIntegration>()
                .IncludePaymentPlanIntegration()
                .IncludeEmployerProduct()
                .IncludePaymentPlan()
                .IncludeBenefits()
                .IncludePauses()
                .IncludePatient()
                .IncludePromoCodeCoupon()
                .AsNoTracking()
                .ToArrayAsync();

            return subscriptions.OrderByDescending(x => x.StartDate).ToArray();
        }

        /// <summary>
        /// <see cref="ISubscriptionService.GetAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Subscription> GetAsync(int id)
        {
            var subscription = await _subscriptionRepository
                .All()
                .ById(id)
                .IncludePauses()
                .IncludePatient()
                .IncludeCancellationRequest()
                .IncludeIntegrations<Subscription, SubscriptionIntegration>()
                .IncludeEmployerProduct()
                .IncludePaymentPlan()
                .IncludePurchasePayors()
                .IncludeBenefits()
                .FirstOrDefaultAsync();     

            if (subscription is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Subscription does not exist.", exceptionParam);
            }

            return subscription;
      
        }

        /// <summary>
        /// Returns by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public async Task<Subscription> GetAsync(int id, ISpecification<Subscription> specification)
        {
            var subscription = await _subscriptionRepository
                .All()
                .ById(id)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();     

            if (subscription is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                
                throw new AppException(HttpStatusCode.NotFound, "Subscription does not exist.", exceptionParam);
            }

            return subscription;

        }

        /// <summary>
        /// <see cref="ISubscriptionService.GetFinishingSubscriptionsAsync(DateTime,int)"/>
        /// </summary>
        /// <param name="date"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Subscription>> GetFinishingSubscriptionsAsync(DateTime date, int practiceId)
        {
            return await _subscriptionRepository
                .All()
                .Active()
                .Renewable()
                .FinishingSubscriptions(date)
                .IncludeCancellationRequest()
                .IncludeIntegrations<Subscription, SubscriptionIntegration>()
                .SubscriptionsRelatedToPractice(practiceId)
                .IncludeEmployerProduct()
                .IncludeRenewalStrategy()
                .IncludePaymentPlan()
                .IncludePatient()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="ISubscriptionService.GetFinishingSubscriptionsAsync(DateTime, DateTime, int)"/>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Subscription>> GetFinishingSubscriptionsAsync(DateTime from, DateTime to, int practiceId)
        {
            return await _subscriptionRepository
                .All()
                .Active()
                .Renewable()
                .FinishingSubscriptions(from, to)
                .IncludeCancellationRequest()
                .IncludeIntegrations<Subscription, SubscriptionIntegration>()
                .SubscriptionsRelatedToPractice(practiceId)
                .IncludeEmployerProduct()
                .IncludeRenewalStrategy()
                .IncludePaymentPlan()
                .IncludePatient()
                .ToArrayAsync();
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionsToCancelAsync(DateTime date, int practiceId)
        {
            return await _subscriptionRepository
                .All()
                .Active()
                .SubscriptionsRelatedToPractice(practiceId)
                .IncludeCancellationRequest()
                .IncludeIntegrations<Subscription, SubscriptionIntegration>()
                .IncludePaymentPlan()
                .IncludePatient()
                .ByCancellationRequest(date)
                .ToArrayAsync();
        }

        /// <summary>
        /// Returns the best subscription candidate for a patient for editing/renewal
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<Subscription> SubscriptionCandidateForModificationAsync(int patientId)
        {
            var subscriptionCandidate = await _subscriptionRepository
                .All()
                .Where(o => o.PatientId == patientId)
                .Active()
                .IncludePaymentPlan()
                .IncludePatient()
                .ToArrayAsync();

            // If there are multiple subscriptions then we do not want to proceed
            if (subscriptionCandidate.Length > 1)
            {
                throw new AppException(HttpStatusCode.Conflict,
                    $"There are multiple active subscriptions for patient, please resolve this conflict before proceeding");
            }

            if (subscriptionCandidate.Length == 1)
            {
                return subscriptionCandidate.First();
            }
            
            // If there are no actives, then we want to grab the most recent canceled item
            var canceledCandidate = await _subscriptionRepository
                .All()
                .Where(o => o.PatientId == patientId)
                .IncludePaymentPlan()
                .IncludePatient()
                .OrderByDescending(x => x.CanceledAt).ThenByDescending(x => x.EndDate).ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (canceledCandidate is null)
            {
                throw new AppException(HttpStatusCode.NotFound,
                    $"This patient has no subscription candidates for modification");
            }
            
            return canceledCandidate;
        }

        /// <summary>
        /// <see cref="ISubscriptionService.GetByIntegrationIdAsync"/>
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        public Task<Subscription> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor, bool activeOnly = true)
        {
            var subscriptions = _subscriptionRepository
                .All()
                .IncludePauses()
                .ByIntegrationId<Subscription, SubscriptionIntegration>(integrationId, vendor, IntegrationPurposes.Payment.Id)
                .IncludeIntegrations<Subscription, SubscriptionIntegration>()
                .IncludeCancellationRequest()
                .IncludePaymentPlan()
                .IncludePatient()
                .IncludePatientSubscriptions();
                
            if (activeOnly)
            {
                return subscriptions.Active().FindAsync();
            }
            
            return subscriptions
                .OrderByDescending(x => x.CreatedAt)
                .Take(1)
                .FindAsync();
        }

        public async Task<Subscription> GetByPaymentIssueId(int paymentIssueId)
        {
            return await _subscriptionRepository
                .All()
                .Active()
                .Where(s => s.Integrations.Any(i => i.Integration.PaymentIssues.Any(p => p.Id == paymentIssueId)))
                .IncludeIntegrations<Subscription, SubscriptionIntegration>()
                .IncludePaymentPlan()
                .IncludePatient()
                .IncludePauses()
                .FindAsync();
        }
    }
}
