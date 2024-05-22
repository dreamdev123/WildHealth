using System;
using System.Threading.Tasks;

namespace WildHealth.Application.Services.SMS
{
    public interface ISMSService
    {
        public Task<string> SendAsync(
            string messagingServiceSidType,
            string to, 
            string body, 
            string universalId, 
            int practiceId, 
            bool? avoidflag = null, 
            DateTime? sendAt = null);
        
        public Task<string> SendAsyncNoFeatureFlag(
            string messagingServiceSidType,
            string to, 
            string body, 
            string universalId, 
            int practiceId);
    }
}