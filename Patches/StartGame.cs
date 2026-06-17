using System.Linq;
using Archipelago.Archipelago;
using Archipelago.Data;
using Archipelago.MultiClient.Net.Enums;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
public class StartGamePatch
{
    [HarmonyPostfix]
    static void Postfix(GameController __instance)
    {
        var session = APClient.Session;
        if (session?.Socket.Connected == true)
        {
            var ids = __instance.SpiritControllers
                .SelectMany(spirit =>
                    spirit.PowerCardControllers
                        .Select(card =>
                        {
                            var nameIfCard = $"{Globals.PLAY_CARD_PREFIX}{card}";
                            return session.Locations.GetLocationIdFromName(Globals.GAME_NAME, nameIfCard);
                        })
                )
                .Where(x => x != -1)
                .ToArray();
            ScoutService.ScoutOnce(ids, HintCreationPolicy.None);
        }
    }
}
