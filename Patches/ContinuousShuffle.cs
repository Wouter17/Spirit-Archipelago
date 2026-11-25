using System;
using System.Linq;
using Archipelago.Archipelago;
using Archipelago.Data;
using Handelabra.SpiritIsland.Engine;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(GameController), nameof(GameController.CheckForGameOver))]
public class ContinuousShufflePatch
{
    [HarmonyPostfix]
    public static void OrderPostfix(GameController __instance)
    {
        if (!ArchipelagoModifiers.prioritisedShuffle)
        {
            return;
        }

        LocationController[] decks = [__instance.MinorPowerDeck, __instance.MajorPowerDeck];
        foreach (var powerDeck in decks)
        {
            if (powerDeck == null)
            {
                return;
            }
            var cardsWithUnlock = powerDeck.CardControllers.Where(card =>
            {
                var locationId = APClient.Session?.Locations.GetLocationIdFromName(Globals.GAME_NAME, $"{Globals.PLAY_CARD_PREFIX}{card.Title}");
                if (locationId == null || APClient.Session?.Locations.AllMissingLocations.Contains(locationId.Value) != true)
                {
                    return false;
                }
                return true;
            });
            if (cardsWithUnlock.Any())
            {
                MoveCardsAction moveCardsAction = __instance.MoveCards(
                    null,
                    MoveCardReason.Debugging,
                    cardsWithUnlock,
                    powerDeck,
                    offset: 0
                );
                var enumerator = __instance.DoAction(moveCardsAction, true, true);
                __instance.ExhaustCoroutine(enumerator);
                __instance.StartCoroutine(enumerator);
            }
        }
    }

    [HarmonyPostfix]
    public static void DeckPostfix(GameController __instance)
    {
        if (!ArchipelagoModifiers.prioritisedShuffle)
        {
            return;
        }

        ValueTuple<LocationController, LocationController>[] decks = [
            (__instance.MinorPowerDiscard, __instance.MinorPowerDeck),
            (__instance.MajorPowerDiscard, __instance.MajorPowerDeck)
        ];
        foreach (var (discard, powerDeck) in decks)
        {
            if (discard == null || powerDeck == null)
            {
                return;
            }

            var missingLocations = APClient.Session?.Locations.AllMissingLocations;
            var cardsWithUnlock = discard.CardControllers.Where(card =>
            {
                var locationId = APClient.Session?.Locations.GetLocationIdFromName(Globals.GAME_NAME, $"{Globals.PLAY_CARD_PREFIX}{card.Title}");
                if (locationId == null || missingLocations?.Contains(locationId.Value) != true)
                {
                    return false;
                }
                return true;
            });

            if (cardsWithUnlock.Any())
            {
                var range = powerDeck.Location.Cards.TakeWhile(c =>
                {
                    var locationId = APClient.Session?.Locations.GetLocationIdFromName(Globals.GAME_NAME, $"{Globals.PLAY_CARD_PREFIX}{c.Title}");
                    return locationId != null && missingLocations?.Contains(locationId.Value) == true;
                }).Count();

                MoveCardsAction moveCardsAction = __instance.MoveCards(
                    null,
                    MoveCardReason.Debugging,
                    cardsWithUnlock,
                    powerDeck,
                    offset: UnityEngine.Random.Range(0, range + 1)
                );
                var enumerator = __instance.DoAction(moveCardsAction, true, true);
                __instance.ExhaustCoroutine(enumerator);
                __instance.StartCoroutine(enumerator);
            }
        }
    }
}