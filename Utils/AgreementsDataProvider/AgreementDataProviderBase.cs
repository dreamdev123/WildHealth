using System;

namespace WildHealth.Application.Utils.AgreementsDataProvider
{
    public abstract class AgreementDataProviderBase
    {
        protected const string Checked = "true";
        protected const string Unchecked = "false";
        
        protected string GetDocumentReferences()
        {
            return Guid.NewGuid().ToString();
        }
    }
}