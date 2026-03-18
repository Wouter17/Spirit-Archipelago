using System.Collections.Generic;
using System.Linq;
using Archipelago.Data;
using Archipelago.MultiClient.Net.Enums;
using Newtonsoft.Json.Linq;

namespace Archipelago.Archipelago;

public static class LocationService
{
    private static readonly Queue<string> queuedLocations = new();

    public static void CheckLocation(string name)
    {
        var session = APClient.Session;
        if (session?.Socket.Connected == true)
        {
            var id = session.Locations.GetLocationIdFromName(Globals.GAME_NAME, name);
            session.Locations.CompleteLocationChecks(id);

            var achieved = session.DataStorage[Scope.Slot, Globals.GOALS_STORE_LOCATION].To<long[]>();
            if (!achieved.Contains(id))
                session.DataStorage[Scope.Slot, Globals.GOALS_STORE_LOCATION] = JArray.FromObject(achieved.Append(id));
        }
        else
        {
            queuedLocations.Enqueue(name);
        }
    }
}
