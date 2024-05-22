namespace WildHealth.Application.Utils.Schedulers;
using WildHealth.TimeKit.Clients.Models.Availability;

public interface IAvailabilityHelper
{
    AvailabilityModel[] FormatResults(AvailabilityModel[] availability, int interval);
}