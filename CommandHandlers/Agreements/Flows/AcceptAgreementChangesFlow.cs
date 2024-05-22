using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Agreements;

namespace WildHealth.Application.CommandHandlers.Agreements.Flows;

public class AcceptAgreementChangesFlow: IMaterialisableFlow
{
    private int AgreementId { get; }
    private AgreementConfirmation[] Confirmations { get; }
    
    public AcceptAgreementChangesFlow(
        int agreementId,
        AgreementConfirmation[] confirmations)
    {
        AgreementId = agreementId;
        Confirmations = confirmations;
    }
    
    public MaterialisableFlowResult Execute()
    {
        var changedConfirmations = Confirmations
            .Where(x => x.AgreementId == AgreementId)
            .Where(x => !x.IsChangesAccepted())
            .ToArray();

        if (changedConfirmations.Any())
        {
            foreach (var confirmation in changedConfirmations)
            {
                confirmation.AcceptChanges();
            }
        }

        return new MaterialisableFlowResult(changedConfirmations.Select(x => x.Updated()));
    }
}