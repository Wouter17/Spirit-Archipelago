using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Archipelago.Archipelago;
using BepInEx.Logging;
using Handelabra.SpiritIsland.Engine.Model;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(Game), nameof(Game.CreateBlightCard), [typeof(string), typeof(IEnumerable<string>), typeof(bool)])]
public class BlightPatch
{
    static readonly ManualLogSource logger = Logger.CreateLogSource("BlightPatch");

    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        var target = AccessTools.Method(
            typeof(PieceFactory),
            nameof(PieceFactory.CreateBlightPieces),
            [typeof(Game), typeof(int)]
        );

        var adjustMethod = AccessTools.Method(
            typeof(BlightPatch),
            nameof(AdjustCount)
        );

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(target))
            {
                // Insert call before CreateBlightPieces
                codes.Insert(i,
                    new CodeInstruction(OpCodes.Call, adjustMethod)
                );

                i++;
            }
        }

        return codes;
    }

    static int AdjustCount(int NumberOfBlight)
    {
        int newBlightCount = Math.Max(1, NumberOfBlight + ArchipelagoModifiers.blightAdjustment);
        logger.LogInfo($"Original blight is {NumberOfBlight}, setting blight to {newBlightCount}");
        return newBlightCount;
    }
}
