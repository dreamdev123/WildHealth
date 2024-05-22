
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.CommandHandlers.Documents.Flows;

public class AddAutomatedDocumentSourceItemFlow : IMaterialisableFlow
{
    private readonly int _automatedDocumentSourceId;
    private readonly string _documentTitle;
    private readonly string _integrationId;
    private readonly IntegrationVendor _integrationVendor;
    private readonly string _integrationPurpose;

    public AddAutomatedDocumentSourceItemFlow(
        int automatedDocumentSourceId,
        string documentTitle,
        string integrationId,
        IntegrationVendor integrationVendor,
        string integrationPurpose)
        {
            _automatedDocumentSourceId = automatedDocumentSourceId;
            _documentTitle = documentTitle;
            _integrationId = integrationId;
            _integrationVendor = integrationVendor;
            _integrationPurpose = integrationPurpose;
        }

    public MaterialisableFlowResult Execute()
    {
        var result = MaterialisableFlowResult.Empty;
        
        var item = new AutomatedDocumentSourceItem()
        {
            AutomatedDocumentSourceId = _automatedDocumentSourceId,
            DocumentTitle = _documentTitle,
            Integrations = new List<AutomatedDocumentSourceItemIntegration>() { new AutomatedDocumentSourceItemIntegration(
                vendor: _integrationVendor,
                purpose: _integrationPurpose, 
                value: _integrationId) }
        };
        
        item.SetStatus(AutomatedDocumentSourceItemStatus.Acknowledged);

        result += item.Added();

        return result;
    }
}