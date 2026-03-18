using System.Linq;
using Archipelago.Archipelago;
using Archipelago.Data;
using BepInEx.Logging;
using Handelabra.SpiritIsland.Engine;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(GameController), nameof(GameController.CheckForGameOver))]
public class OnUpdatePatch
{
    private static readonly ManualLogSource logger = Logger.CreateLogSource("OnUpdatePatch");
    static void Postfix(GameController __instance)
    {
        logger.LogInfo($"moving cards (update)");
        var disallowedCards = __instance.MinorPowerDeck.CardControllers
            .Concat(__instance.MinorPowerDiscard.CardControllers)
            .Concat(__instance.MajorPowerDeck.CardControllers)
            .Concat(__instance.MajorPowerDiscard.CardControllers)
            .Where(card => ArchipelagoModifiers.LockedCards().Contains(card.Title));
        if (disallowedCards.Any())
        {
            MoveCardsAction moveCardsAction = __instance.MoveCards(null, MoveCardReason.Debugging, disallowedCards, __instance.OutOfGame);
            var enumerator = __instance.DoAction(moveCardsAction, true, true);
            __instance.ExhaustCoroutine(enumerator);
            __instance.StartCoroutine(enumerator);
        }

        var missingLocations = APClient.Session?.Locations.AllMissingLocations.ToHashSet();

        var allowedMinorCards = __instance.OutOfGame.CardControllers.Where(card => card.IsMinorPowerCard && !ArchipelagoModifiers.LockedCards().Contains(card.Title));
        if (allowedMinorCards.Any())
        {
            var range = __instance.MinorPowerDeck.Location.Cards.TakeWhile(c =>
            {
                var locationId = APClient.Session?.Locations.GetLocationIdFromName(Globals.GAME_NAME, $"{Globals.PLAY_CARD_PREFIX}{c.Title}");
                return locationId != null && missingLocations?.Contains(locationId.Value) == true;
            }).Count();
            MoveCardsAction moveCardsAction = __instance.MoveCards(
                null,
                MoveCardReason.Debugging,
                allowedMinorCards,
                __instance.MinorPowerDeck,
                offset: ArchipelagoModifiers.prioritisedShuffle ? UnityEngine.Random.Range(0, range + 1) : null
            );
            var enumerator = __instance.DoAction(moveCardsAction, true, true);
            __instance.ExhaustCoroutine(enumerator);
            __instance.StartCoroutine(enumerator);
        }

        var allowedMajorCards = __instance.OutOfGame.CardControllers.Where(card => card.IsMajorPowerCard && !ArchipelagoModifiers.LockedCards().Contains(card.Title));
        if (allowedMajorCards.Any())
        {
            var range = __instance.MajorPowerDeck.Location.Cards.TakeWhile(c =>
            {
                var locationId = APClient.Session?.Locations.GetLocationIdFromName(Globals.GAME_NAME, $"{Globals.PLAY_CARD_PREFIX}{c.Title}");
                return locationId != null && missingLocations?.Contains(locationId.Value) == true;
            }).Count();
            MoveCardsAction moveCardsAction = __instance.MoveCards(
                null,
                MoveCardReason.Debugging,
                allowedMajorCards,
                __instance.MajorPowerDeck,
                offset: ArchipelagoModifiers.prioritisedShuffle ? UnityEngine.Random.Range(0, range + 1) : null
            );
            var enumerator = __instance.DoAction(moveCardsAction, true, true);
            __instance.ExhaustCoroutine(enumerator);
            __instance.StartCoroutine(enumerator);
        }
        var remaining = __instance.MinorPowerDeck.CardControllers
            .Concat(__instance.MinorPowerDiscard.CardControllers)
            .Concat(__instance.MajorPowerDeck.CardControllers)
            .Concat(__instance.MajorPowerDiscard.CardControllers);
    }
}
