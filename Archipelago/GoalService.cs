using System.Collections.Generic;
using System.Linq;
using Archipelago.Data;
using Archipelago.UI;

namespace Archipelago.Archipelago;

public static class GoalService
{
    public static HashSet<long> goals { get; private set; } = [];

    private static int requiredGoals = 1;

    public static void CheckGoalCompletion()
    {
        if (APClient.Session != null)
        {
            SimpleUI.SetCheckedLocations(APClient.Session.Locations.AllLocationsChecked);
        }

        if (APClient.Session?.Socket.Connected == true && requiredGoals <= APClient.Session.Items.AllItemsReceived.Count(item => item.ItemName == Globals.VICTORY_ITEM_NAME))
        {
            APClient.Session.SetGoalAchieved();
        }
    }
    public static void Initialise(IEnumerable<long> goalIds, int requiredGoals) {
        goals = goalIds.ToHashSet();
        GoalService.requiredGoals = requiredGoals;
    }
}
