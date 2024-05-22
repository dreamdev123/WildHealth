using System.Threading.Tasks;
using WildHealth.Twilio.Clients.Models.Lookup;

namespace WildHealth.Application.Services.SMS;

public interface ISMSLookupService
{

    Task<LookupResponseModel> LookupE164Async(string phoneNumber);
    Task<LookupResponseModel> LookupNationalFormatAsync(string phoneNumber, string countryCode = "US");
}