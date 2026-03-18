using System;
using Archipelago.Archipelago;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(SpiritController), nameof(SpiritController.GetCardPlaysPerTurn))]
public class CardplaysPatch
{
    [HarmonyPostfix]
    static int Postfix(int cardplays)
    {
        return Math.Max(1, cardplays + ArchipelagoModifiers.cardplaysAdjustment);
    }
}
