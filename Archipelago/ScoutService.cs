using System.Collections.Concurrent;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace Archipelago.Archipelago;

public static class ScoutService
{
    private static readonly ConcurrentDictionary<long, ScoutedItemInfo> scouted = new();

    public static ScoutedItemInfo? Scout(long id)
    {
        if (scouted.TryGetValue(id, out var res))
            return res;

        var session = APClient.Session;
        if (session?.Locations
            .ScoutLocationsAsync(
                APClient.HintCards > HintCardsOption.None ? HintCreationPolicy.CreateAndAnnounceOnce : HintCreationPolicy.None,
                [id])
            .Result.TryGetValue(id, out var resScout) ?? false)
        {
            scouted.TryAdd(id, resScout);
            return resScout;
        }

        return null;
    }
}
