using System;
using Archipelago.Archipelago;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(SpiritController), nameof(SpiritController.GetEnergyPerTurn))]
public class EnergyPatch
{
    [HarmonyPostfix]
    static int Postfix(int energy)
    {
        return Math.Max(0, energy + ArchipelagoModifiers.energyAdjustment);
    }
}
