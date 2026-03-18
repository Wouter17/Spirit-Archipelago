using System.Collections.Generic;
using System.Linq;
using Archipelago.Archipelago;
using Archipelago.Data;
using Archipelago.MultiClient.Net.Enums;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(PowerController), nameof(PowerController.Body), MethodType.Getter)]
class PowercardBodyPatch
{
    static void Postfix(PowerCardController __instance, ref IEnumerable<string> __result)
    {
        if (!APClient.SpoilLocations)
            return;

        var locationId = APClient.Session?.Locations.GetLocationIdFromName(Globals.GAME_NAME, $"{Globals.PLAY_CARD_PREFIX}{__instance.Card.Title}");
        if (locationId == null || APClient.Session?.Locations.AllMissingLocations.Contains(locationId.Value) != true)
        {
            return;
        }

        var scoutedItemInfo = ScoutService.Scout(locationId.Value);
        if (scoutedItemInfo == null)
        {
            return;
        }

        string color = "#14DE9E";
        var flags = scoutedItemInfo.Flags;
        if (flags.HasFlag(ItemFlags.Advancement) && flags.HasFlag(ItemFlags.NeverExclude))
        {
            color = "#FFD700";
        }
        else if (flags.HasFlag(ItemFlags.Trap) && flags.HasFlag(ItemFlags.NeverExclude))
        {
            color = "#FF7F00";
        }
        else if (flags.HasFlag(ItemFlags.Advancement))
        {
            color = "#BC51E0";
        }
        else if (flags.HasFlag(ItemFlags.NeverExclude))
        {
            color = "#2B67FF";
        }
        else if (flags.HasFlag(ItemFlags.Trap))
        {
            color = "#D63A22";
        }

        var text = $"Unlocks: <color={color}>{scoutedItemInfo.ItemDisplayName} ({scoutedItemInfo.Player})</color>";
        if (__result == null)
        {
            __result = [text];
        }
        else
        {
            __result = __result.Append(text);
        }
    }
}
