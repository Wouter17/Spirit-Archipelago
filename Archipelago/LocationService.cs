using System.Collections.Generic;
using System.Linq;
using Archipelago.Data;

namespace Archipelago.Archipelago;

public static class LocationService
{
    private static readonly Queue<string> queuedLocations = new();

    public static void CheckLocation(params string[] names)
    {
        var session = APClient.Session;
        if (session?.Socket.Connected == true)
        {
            var ids = names.Select(name => session.Locations.GetLocationIdFromName(Globals.GAME_NAME, name)).Where(id => id != -1).ToArray();
            session.Locations.CompleteLocationChecks(ids);
        }
        else
        {
            foreach (var name in names)
            {
                queuedLocations.Enqueue(name);
            }
        }
    }
}
