using System.Text.RegularExpressions;
using Archipelago.Archipelago;
using Handelabra.SpiritIsland.View;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(NewGameViewController), nameof(NewGameViewController.AllowContentInGame))]
public class ContentPatch
{
    private static readonly Regex sWhitespace = new(@"[^A-Za-z]");
    static void Postfix(string identifier, ref bool __result)
    {
        if (identifier.Contains("_"))
        {
            __result = !ArchipelagoModifiers.LockedAspects().Contains(sWhitespace.Replace(identifier.ToLower(), ""));
        }
        else
        {
            __result = !ArchipelagoModifiers.LockedSpirits().Contains(sWhitespace.Replace(identifier.ToLower(), ""));
        }
    }
}
