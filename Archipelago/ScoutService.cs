using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace Archipelago.Archipelago;

public static class ScoutService
{
    private static readonly ConcurrentDictionary<long, (ScoutedItemInfo, HintCreationPolicy)> scouted = new();
    private static readonly ConcurrentDictionary<long, byte> inFlight = new();

    public static ScoutedItemInfo? Scout(long id)
    {
        return Scout(id, APClient.HintCards > HintCardsOption.None ? HintCreationPolicy.CreateAndAnnounceOnce : HintCreationPolicy.None);
    }

    public static ScoutedItemInfo? Scout(long id, HintCreationPolicy policy)
    {
        ScoutedItemInfo? result = null;
        if (scouted.TryGetValue(id, out var res))
        {
            result = res.Item1;
            if (res.Item2 >= policy)
                return result;
        }

        var session = APClient.Session;
        if (session == null)
            return result;

        if (!inFlight.TryAdd(id, 0))
            return result;

        var scoutTask = session.Locations.ScoutLocationsAsync(policy, [id]);
        scoutTask.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result.TryGetValue(id, out var resScout))
            {
                scouted.TryAdd(id, (resScout, policy));
            }
        });

        Task.WhenAny(scoutTask, Task.Delay(TimeSpan.FromSeconds(3))).ContinueWith(task =>
        {
            inFlight.TryRemove(id, out var _);
        });


        return result;
    }

    /// <summary>
    /// Scout call for an array of ids, where there is no guarentee the hints will get created.
    /// </summary>
    /// <param name="ids">The ids of the locations to scout</param>
    /// <param name="policy">The scouting policy for all locations</param>
    public static void ScoutOnce(long[] ids, HintCreationPolicy policy)
    {
        var session = APClient.Session;
        if (session == null)
            return;

        var scoutTask = session.Locations.ScoutLocationsAsync(policy, ids);
        scoutTask.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                foreach (var id in ids)
                {
                    task.Result.TryGetValue(id, out var resScout);
                    scouted.TryAdd(id, (resScout, policy));
                }
            }
        });
    }
}
