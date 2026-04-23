using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Archipelago.Archipelago;
using BepInEx.Logging;
using Handelabra.SpiritIsland.Engine;
using Handelabra.SpiritIsland.Engine.Controller;
using Handelabra.SpiritIsland.Engine.Model;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(GameController), nameof(GameController.GetElementsForSpirit))]
public class ElementsPatch
{
    static void Prefix(GameController __instance)
    {
        if (ArchipelagoModifiers.ElementsAdjustment.Values.Sum() == 0)
            return;

        foreach (var controller in __instance.SpiritControllers)
        {
            var action = new GainElementsAction(__instance, null, controller, 
                ArchipelagoModifiers.ElementsAdjustment.SelectMany(kvp => Enumerable.Repeat(kvp.Key, kvp.Value)),
                false, false
            );
            var enumerator = __instance.DoAction(action, true, true);
            __instance.ExhaustCoroutine(enumerator);
            __instance.StartCoroutine(enumerator);
        }
        ArchipelagoModifiers.ElementsAdjustment.Clear();
    }
}

[HarmonyPatch(typeof(GameController), nameof(GameController.TimePasses))]
public class ElementsPatchTimePasses
{
    static void Postfix()
    {
        ArchipelagoModifiers.ElementsAdjustment.Clear();
    }
}

[HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
public class ElementsPatchStartGame
{
    static void Postfix()
    {
        ArchipelagoModifiers.ElementsAdjustment.Clear();
    }
}