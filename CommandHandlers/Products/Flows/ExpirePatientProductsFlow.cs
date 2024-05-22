using System;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.CommandHandlers.Products.Flows;

public class ExpirePatientProductsFlow : IMaterialisableFlow
{
    private readonly ICollection<PatientProduct> _productsToExpire;
    private readonly string _reason;
    private readonly DateTime _utcNow;

    public ExpirePatientProductsFlow(ICollection<PatientProduct> productsToExpire, string reason, DateTime utcNow)
    {
        _productsToExpire = productsToExpire;
        _reason = reason;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        return new MaterialisableFlowResult(ExpireActions());
    }

    private IEnumerable<EntityAction> ExpireActions()
    {
        foreach (var patientProduct in _productsToExpire)
        {
            if (patientProduct.UsedAt.HasValue) continue;
            
            patientProduct.ExpireProduct(_reason, _utcNow);
            yield return patientProduct.Updated();
        }
    }
}