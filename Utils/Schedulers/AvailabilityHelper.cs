using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.TimeKit.Clients.Models.Availability;

namespace WildHealth.Application.Utils.Schedulers;

public class AvailabilityHelper : IAvailabilityHelper
{
    private IDictionary<int, int[]> _acceptableIntervalStartTimes = new Dictionary<int, int[]>()
    {
        { 60, new [] { 0, 30} },
        { 30, new [] { 0, 30} },
        { 15, new [] { 0, 15, 30, 45} },
        { 5, new [] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 } },
    };
    
    /// <summary>
    /// This method will utilize the following O(n) algorithm to get appropriate items for availability
    /// 1. Order the results to go from most recent to latest, can assume adjacent availability blocks are next to each other
    /// 2. Go through each item in the list, evaluate if this is an "adjacent
    /// </summary>
    /// <param name="availability"></param>
    /// <param name="interval"></param>
    /// <returns></returns>
    public AvailabilityModel[] FormatResults(AvailabilityModel[] availability, int interval)
    {
        // if (interval == AvailabilityConstants.DefaultAvailabilityInterval)
        // {
        //     return availability;
        // }
        //
        // return availability
        //     .Where(x => x.Start.Minute % interval == 0)
        //     .ToArray();
        
        var acceptableStartTimes = _acceptableIntervalStartTimes[interval];
        
        var eligibleAvailabilityOrdered = availability
            .Where(o => acceptableStartTimes.Contains(o.Start.Minute))
            .OrderBy(o => o.Start);
        
        return eligibleAvailabilityOrdered.ToArray();
    }

    private bool Conflicts(AvailabilityModel thisItem, AvailabilityModel priorItem)
    {
        return priorItem.End > thisItem.Start;
    }
    
}