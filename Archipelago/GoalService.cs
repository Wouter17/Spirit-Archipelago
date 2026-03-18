using System.Collections.Generic;
using System.Linq;
using Archipelago.UI;

namespace Archipelago.Archipelago;

public static class GoalService
{
    public static HashSet<long> goals { get; private set; } = [-1];

    public static void CheckGoalCompletion(long[] checkedLocations)
    {
        if (APClient.Session != null)
        {
            SimpleUI.SetCheckedLocations(APClient.Session.Locations.AllLocationsChecked);
        }

        if (APClient.Session?.Socket.Connected == true && goals.IsSubsetOf(checkedLocations))
        {
            APClient.Session.SetGoalAchieved();
        }
    }
    public static void Initialise(IEnumerable<long> goalIds) => goals = goalIds.ToHashSet();
}
