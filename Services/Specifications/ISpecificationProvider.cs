using WildHealth.Infrastructure.Data.Specifications.Enums;
using WildHealth.Shared.Data.Entities;
using WildHealth.Shared.Data.Helpers;

namespace WildHealth.Application.Services.Specifications
{
    public interface ISpecificationProvider
    {
        ISpecification<T> Get<T>(SpecificationsEnum spec) where T : BaseEntity;
    }
}
