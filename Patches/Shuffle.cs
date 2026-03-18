using System.Collections.Generic;
using System.Linq;
using Archipelago.Archipelago;
using Archipelago.Data;
using Handelabra.SpiritIsland.Engine.Controller;
using Handelabra.SpiritIsland.Engine.Model;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(ShuffleLocationAction), nameof(ShuffleLocationAction.DoAction))]
class ShufflePatch
{
    static readonly System.Reflection.FieldInfo cardsField =
        AccessTools.Field(typeof(Location), "_cards");

    static void Postfix(ShuffleLocationAction __instance)
    {
        if (!ArchipelagoModifiers.prioritisedShuffle)
            return;

        var location = __instance.LocationToShuffle.Location;
        if (location != __instance.GameController.MinorPowerDeck.Location && location != __instance.GameController.MajorPowerDeck.Location)
            return;

        if (cardsField.GetValue(location) is not List<Card> cards)
        {
            return;
        }

        var missingLocations = APClient.Session?.Locations.AllMissingLocations.ToHashSet();

        var uncheckedCards = new List<Card>(cards.Count);
        var otherCards = new List<Card>(cards.Count);

        foreach (var card in cards)
        {
            var locationId = APClient.Session?.Locations
                .GetLocationIdFromName(Globals.GAME_NAME, $"{Globals.PLAY_CARD_PREFIX}{card.Title}");

            if (locationId != null && missingLocations?.Contains(locationId.Value) == true)
                uncheckedCards.Add(card);
            else
                otherCards.Add(card);
        }

        cards.Clear();
        cards.AddRange(uncheckedCards);
        cards.AddRange(otherCards);
    }
}
