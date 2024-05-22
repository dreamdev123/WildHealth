using System;
using System.Collections.Generic;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Specifications.Enums;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Entities;
using WildHealth.Shared.Data.Helpers;

namespace WildHealth.Application.Services.Specifications
{
    public class SpecificationProvider: ISpecificationProvider
    {
        private static readonly Dictionary<SpecificationsEnum, ISpecification<Patient>> PatientSpecificationsMap = new Dictionary<SpecificationsEnum, ISpecification<Patient>>
        {
            { SpecificationsEnum.PatientInitialConsultSpecification, PatientSpecifications.PatientInitialConsultSpecification },
            { SpecificationsEnum.PatientUserSpecification, PatientSpecifications.PatientUserSpecification },
            { SpecificationsEnum.SubmitPatientSpecification, PatientSpecifications.SubmitPatientSpecification },
            { SpecificationsEnum.UpdatePatientSpecification, PatientSpecifications.UpdatePatientSpecification }
        };

        private static readonly Dictionary<Type, dynamic> EntitiesSpecifications = new Dictionary<Type, dynamic>()
        {
            { typeof(Patient), PatientSpecificationsMap }
        };

        public ISpecification<T> Get<T>(SpecificationsEnum spec) where T: BaseEntity
        {
            var entitySpecificationsExist = EntitiesSpecifications.TryGetValue(typeof(T), out var entitySpecifications);

            if (!entitySpecificationsExist)
            {
                throw new ArgumentException("Can't get entity specification");
            }

            var specificationExist = ((entitySpecifications as Dictionary<SpecificationsEnum, ISpecification<T>>)!).TryGetValue(spec, out var specification);

            if (!specificationExist)
            {
                throw new ArgumentException("Can't get specification");
            }

            return specification!;
        }
    }
}
