using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using WildHealth.Application.Events.Patients;
using WildHealth.ClarityCore.Models.HealthScore;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Models.Patient;
using WildHealth.IntegrationEvents.Common.Payloads;
using WildHealth.IntegrationEvents.Patients.Payloads;
using WildHealth.IntegrationEvents.Users.Payloads;

namespace WildHealth.Application.IntegrationEvents.Mappers
{
    public class IdentifiablePayload : Profile
    {
        public IdentifiablePayload()
        {
            AssignPayloadMappings<PatientCreatedPayload>();
            AssignPayloadMappings<PatientUpdatedPayload>();
            AssignPayloadMappings<UserAuthenticatedPayload>();
            AssignPayloadMappings<UserUpdatedPayload>();
            AssignPayloadMappings<IdentifyPayload>();
            
            CreateMap<PatientRegisteredEvent, PatientRegisteredPayload>();
        }

        private void AssignPayloadMappings<T>() where T : IPayloadWithIdentify
        {
            CreateMap<User, T>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.BillingAddress.ToString()))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.GenderName))
                .ForMember(dest => dest.TrackingIdentifier, opt => opt.MapFrom(src => src.TrackingIdentity()))
                .ForMember(dest => dest.AssignedHealthCoachId, opt => opt.MapFrom(src => src.AssignedHealthCoachId))
                .ForMember(dest => dest.AssignedHealthCoach, opt => opt.MapFrom(src => src.AssignedHealthCoach))
                .ForMember(dest => dest.AssignedDoctorId, opt => opt.MapFrom(src => src.AssignedProviderId))
                .ForMember(dest => dest.AssignedDoctor, opt => opt.MapFrom(src => src.AssignedProvider))
                .ForMember(dest => dest.AssignedPod, opt => opt.MapFrom(src => src.AssignedPod))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.ShippingAddress.ToString()))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => Format(src.Birthday)))
                .ForMember(dest => dest.EverSubscribed, opt => opt.MapFrom(src => src.HasSubscribed ))
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => Format(src.CreatedAt) ))
                .ForMember(dest => dest.MarketingSms, opt => opt.MapFrom(src => src.Options.MarketingSMS ))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ))
                .ForMember(dest => dest.EmailOriginal, opt => opt.MapFrom(src => src.Email ))
                .ForMember(dest => dest.EverSubscribed, opt => opt.MapFrom(src => src.HasSubscribed))
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => Format(src.CreatedAt)))
                .ForMember(dest => dest.MarketingSms, opt => opt.MapFrom(src => src.Options.MarketingSMS))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.EmailOriginal, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.BillingAddress.City))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.BillingAddress.State))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.BillingAddress.Country))
                .ForMember(dest => dest.EmployerTag, opt => opt.MapFrom(src => src.EmployerProductKey));

            CreateMap<Patient, T>()
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.LocationId))
                .ForMember(dest => dest.Referral, opt => opt.MapFrom(src => PatientDomain.Create(src).LeadSourceName))
                .ForMember(dest => dest.ReferralOtherInput, opt => opt.MapFrom(src => PatientDomain.Create(src).LeadSourceNameOther))
                .ForMember(dest => dest.ReferralPodcastInput, opt => opt.MapFrom(src => PatientDomain.Create(src).LeadSourceNamePodcast))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => PatientDomain.Create(src).GenderName))
                .ForMember(dest => dest.DateCancelled, opt => opt.MapFrom(src => Format(PatientDomain.Create(src).DateCancelled)))
                .ForMember(dest => dest.DateSubscribed, opt => opt.MapFrom(src => Format(PatientDomain.Create(src).DateSubscribed)))
                .ForMember(dest => dest.EverSubscribed, opt => opt.MapFrom(src => src.User.HasSubscribed))
                .ForMember(dest => dest.SubscriptionEndDate,
                    opt => opt.MapFrom(src => Format(src.CurrentSubscription.EndDate)))
                .ForMember(dest => dest.MobileInstalled, opt => opt.MapFrom(src => PatientDomain.Create(src).IsMobileAppInstalled()))
                .ForMember(dest => dest.Tags,
                    opt => opt.MapFrom(src => src.TagRelations.Select(tagRelation => tagRelation.Tag.Name)))
                .ForMember(dest => dest.AssignedHealthCoachEmail,
                    opt => opt.MapFrom(src=>  GetHealthCoach(src)));
               
            CreateMap<Subscription, T>()
                .ForMember(dest => dest.AcValue, opt => opt.MapFrom(src => src.PlanType))
                .ForMember(dest => dest.PlanType, opt => opt.MapFrom(src => src.PlanType))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.ToString()))
                .ForMember(dest => dest.Cadence, opt => opt.MapFrom(src => src.DetermineCadence()))
                .ForMember(dest => dest.PlanPlatform, opt => opt.MapFrom(src => src.DeterminePlatform()))
                .ForMember(dest => dest.PromoCode, opt => opt.MapFrom(src => src.PromoCode))
                .ForMember(dest => dest.ActiveSubscription, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Insurance, opt => opt.MapFrom(src => src.PaymentPrice.IsInsurance()));

            CreateMap<InputsAggregator, T>()
                .ForMember(dest => dest.ExerciseGoal, opt => opt.MapFrom(src => src.ExerciseGoal))
                .ForMember(dest => dest.DietChoice, opt => opt.MapFrom(src => src.DietChoice))
                .ForMember(dest => dest.ExerciseActivitiesFrequency, opt => opt.MapFrom(src => src.ExerciseActivitiesFrequency))
                .ForMember(dest => dest.MeditationFrequency, opt => opt.MapFrom(src => src.MeditationFrequency))
                .ForMember(dest => dest.Rem, opt => opt.MapFrom(src => src.Rem))
                .ForMember(dest => dest.DeepSleep, opt => opt.MapFrom(src => src.DeepSleep))
                .ForMember(dest => dest.Sleep, opt => opt.MapFrom(src => src.Sleep))
                .ForMember(dest => dest.FastingRegularly, opt => opt.MapFrom(src => src.FastingRegularly))
                .ForMember(dest => dest.CancerScreeningCompleted, opt => opt.MapFrom(src => src.CancerScreeningCompleted))
                .ForMember(dest => dest.ChronologicalAge, opt => opt.MapFrom(src => src.ChronologicalAge))
                .ForMember(dest => dest.RealAge, opt => opt.MapFrom(src => src.RealAge))
                .ForMember(dest => dest.BiologicalAge, opt => opt.MapFrom(src => src.BiologicalAge))
                .ForMember(dest => dest.Ethnicity, opt => opt.MapFrom(src => src.Ethnicity))
                .ForMember(dest => dest.SmokingCategory, opt => opt.MapFrom(src => src.SmokingCategory))
                .ForMember(dest => dest.DiabetesType, opt => opt.MapFrom(src => src.DiabetesType))
                .ForMember(dest => dest.FamilyHeartAttack, opt => opt.MapFrom(src => src.FamilyHeartAttack))
                .ForMember(dest => dest.ChronicKidneyDisease, opt => opt.MapFrom(src => src.ChronicKidneyDisease))
                .ForMember(dest => dest.AtrialFibrillation, opt => opt.MapFrom(src => src.AtrialFibrillation))
                .ForMember(dest => dest.BloodPressureTreatment, opt => opt.MapFrom(src => src.BloodPressureTreatment))
                .ForMember(dest => dest.RheumatoidArthritis, opt => opt.MapFrom(src => src.RheumatoidArthritis))
                .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
                ;

            CreateMap<IEnumerable<AddOn>, T>()
                .ForMember(dest => dest.DnaKit, opt => opt.MapFrom(src => src == null ? false : src.Any(o => OrderType.Dna.Equals(o.OrderType))))
                .ForMember(dest => dest.EpiKit, opt => opt.MapFrom(src => src == null ? false : src.Any(o => OrderType.Epigenetic.Equals(o.OrderType))))
                .ForMember(dest => dest.LabKit, opt => opt.MapFrom(src => src == null ? false : src.Any(o => OrderType.Lab.Equals(o.OrderType))));

            CreateMap<AddOn[], T>()
                .ForMember(dest => dest.DnaKit, opt => opt.MapFrom(src => src == null ? false : src.Any(o => OrderType.Dna.Equals(o.OrderType))))
                .ForMember(dest => dest.EpiKit, opt => opt.MapFrom(src => src == null ? false : src.Any(o => OrderType.Epigenetic.Equals(o.OrderType))))
                .ForMember(dest => dest.LabKit, opt => opt.MapFrom(src => src == null ? false : src.Any(o => OrderType.Lab.Equals(o.OrderType))));

            CreateMap<OrderType[], T>()
                .ForMember(dest => dest.DnaKit, opt => opt.MapFrom(src => src == null ? false : src.Contains(OrderType.Dna)))
                .ForMember(dest => dest.EpiKit, opt => opt.MapFrom(src => src == null ? false : src.Contains(OrderType.Epigenetic)))
                .ForMember(dest => dest.LabKit, opt => opt.MapFrom(src => src == null ? false : src.Contains(OrderType.Lab)));

            CreateMap<UserIdentity, T>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.User.BillingAddress.ToString()))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.User.Gender));

            CreateMap<AppointmentSummaryModel, T>()
                .ForMember(dest => dest.MembershipVisits, opt => opt.MapFrom(src => src.CompletedProviderMembershipVisits))
                .ForMember(dest => dest.MembershipVisitsRemaining, opt => opt.MapFrom(src => src.AvailableProviderMembershipVisits))
                .ForMember(dest => dest.VisitsCredits, opt => opt.MapFrom(src => src.CompletedAdditionalProviderVisits))
                .ForMember(dest => dest.VisitsCreditsRemaining, opt => opt.MapFrom(src => src.AvailableAdditionalProviderVisits));

            CreateMap<HealthScoreResponseModel, T>()
                .ForMember(dest => dest.CurrentHealthScore, opt => opt.MapFrom(src => src.PatientScore.Score));

            CreateMap<IWebHostEnvironment, T>()
                .ForMember(dest => dest.Environment, opt => opt.MapFrom(src => GetEnvironment(src)));
        }
        
        public static string? Format(DateTime? dateTime)
        {
            if(dateTime is null) {
                return null;
            }

            if(dateTime == DateTime.MinValue) {
                return null;
            }

            return dateTime.Value.ToString("yyyy/MM/dd");
        }
        
        private string GetEnvironment(IWebHostEnvironment src)
        {
            return src.IsDevelopment() ? "development" : "production";
        }

        private string? GetHealthCoach(Patient patient)
        {
            return patient.GetHealthCoach()?.User?.Email;
        }
    }
}