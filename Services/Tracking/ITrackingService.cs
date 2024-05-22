using System;
using System.Threading.Tasks;
using WildHealth.Common.Models.Tracking;

namespace WildHealth.Application.Services.Tracking;

public interface ITrackingService
{
    Task<TrackingModel?> GetTrackingByUuid(Guid uuid);
}