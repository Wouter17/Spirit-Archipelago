using System.Collections.Generic;
using System.Linq;
using Archipelago.Archipelago;
using BepInEx.Logging;
using Handelabra.SpiritIsland.Engine;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(GameController), nameof(GameController.GameOver))]
public class EndOfGamePatch
{
    static readonly ManualLogSource logger = Logger.CreateLogSource("EndOfGamePatch");
    static readonly HashSet<EndingResult> wins = [EndingResult.WinSacrificeVictory, EndingResult.WinTerrorVictory, EndingResult.WinScenario, EndingResult.WinInvadersDestroyed];
    static void Prefix(GameController __instance, EndingResult endingResult)
    {
        logger.LogInfo($"Game end");

        if (wins.Contains(endingResult))
        {
            logger.LogInfo("Game won!");
            var spirits = __instance.SpiritControllers.Select(sc => sc.TitleWithAspect).ToList();
            logger.LogInfo($"Spirits that won are {string.Join(",", spirits)}");
            foreach (var adversary in __instance.Game.Adversaries ?? [])
            {
                foreach (var spirit in spirits)
                {
                    APClient.AdversaryDefeated(adversary.Title, adversary.Level.GetValueOrDefault(), spirit);
                }
            }
        }
        else
        {
            APClient.GameLost();
        }
    }
}
