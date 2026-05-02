using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.Data;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.UI;
using BepInEx.Logging;
using Handelabra.SpiritIsland.Engine;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Logger = BepInEx.Logging.Logger;

namespace Archipelago.Archipelago;

public enum HintCardsOption
{
    None = 0,
    OnHover = 1,
    Always = 2
}

public static class APClient
{
    internal static readonly ManualLogSource logger = Logger.CreateLogSource("APClient");
    public static ArchipelagoSession? Session { get; private set; }
    public static DeathLinkService? DeathLinkService { get; private set; }
    public static bool SpoilLocations { get; private set; } = true;
    public static HintCardsOption HintCards { get; private set; } = HintCardsOption.Always;

    public static int Deathlink { get; private set; } = 0;
    private static readonly Queue<string> queuedLocations = new();
    private static bool isInitializing = true;

    public static async Task<string?> Connect(string server, string user, string password)
    {
        Session = ArchipelagoSessionFactory.CreateSession(server);
        DeathLinkService = Session.CreateDeathLinkService();

        InitialiseSession(Session, DeathLinkService);
        RoomInfoPacket _ = await Session.ConnectAsync();

        LoginResult result;
        try
        {
            result = await Session.LoginAsync(
                Globals.GAME_NAME,
                user,
                ItemsHandlingFlags.AllItems,
                password: password
            );
        }
        catch (Exception e)
        {
            result = new LoginFailure(e.GetBaseException().Message);
        }

        if (!result.Successful)
        {
            var failure = (LoginFailure)result;
            string errorMessage = $"Failed to connect to {server} as {user}: ";
            foreach (string err in failure.Errors) errorMessage += $"{err}\n    ";
            foreach (var err in failure.ErrorCodes) errorMessage += $"{err}\n    ";
            return errorMessage;
        }

        var loginSuccess = (LoginSuccessful)result;

        // Initialise modifiers
        var gottenItems = Session.Items.AllItemsReceived.Select(i => i.ItemName);
        logger.LogDebug($"Gotten items: {string.Join(", ", gottenItems)}");
        logger.LogDebug($"SlotData: {loginSuccess.SlotData.Join()}");

        ArchipelagoModifiers.spiritShards =
            Convert.ToInt32(loginSuccess.SlotData["spirit_shards"]);

        ArchipelagoModifiers.energyAdjustment =
            Convert.ToInt32(loginSuccess.SlotData["base_energy_offset"]) +
            gottenItems.Count(name => name == Globals.PLUS_ENERGY_NAME);

        ArchipelagoModifiers.cardplaysAdjustment =
            Convert.ToInt32(loginSuccess.SlotData["base_cardplay_offset"]) +
            gottenItems.Count(name => name == Globals.PLUS_CARDPLAYS_NAME);

        ArchipelagoModifiers.blightAdjustment =
            Convert.ToInt32(loginSuccess.SlotData["base_blight_offset"]) +
            gottenItems.Count(name => name == Globals.PLUS_BLIGHT_NAME);

        ArchipelagoModifiers.BaseLockedCards =
            ((JArray)loginSuccess.SlotData["base_locked_cards"]).Values<string>().OfType<string>().ToHashSet();

        ArchipelagoModifiers.BaseLockedSpirits =
            ((JArray)loginSuccess.SlotData["base_locked_spirits"]).Values<string>().OfType<string>().ToHashSet();

        ArchipelagoModifiers.BaseLockedAspects =
            ((JArray)loginSuccess.SlotData["base_locked_aspects"]).Values<string>().OfType<string>().ToHashSet();

        ArchipelagoModifiers.prioritisedShuffle =
            Convert.ToBoolean(loginSuccess.SlotData["prioritised_shuffle"]);

        SpoilLocations = Convert.ToBoolean(loginSuccess.SlotData["spoil_locations"]);

        var hintCardsValue = Convert.ToInt32(loginSuccess.SlotData["hint_cards"]);
        HintCards = Enum.IsDefined(typeof(HintCardsOption), hintCardsValue)
            ? (HintCardsOption)hintCardsValue
            : throw new ArgumentOutOfRangeException();

        var goals = ((JArray)loginSuccess.SlotData["goals"])
            .Values<string>()
            .Select(g => Session.Locations.GetLocationIdFromName(Globals.GAME_NAME, g))
            .ToHashSet();
        GoalService.Initialise(goals);

        Session.DataStorage[Scope.Slot, Globals.GOALS_STORE_LOCATION].Initialize(JArray.FromObject(new int[] { }));
        Session.DataStorage[Scope.Slot, Globals.GOALS_STORE_LOCATION].OnValueChanged += (_, newVal, _) =>
        {
            var goalIds = newVal?.ToObject<long[]>() ?? Array.Empty<long>();
            GoalService.CheckGoalCompletion(goalIds);
        };
        GoalService.CheckGoalCompletion(Session.DataStorage[Scope.Slot, Globals.GOALS_STORE_LOCATION]);

        Deathlink = Convert.ToInt32(loginSuccess.SlotData["deathlink"]);
        if (Deathlink == 1)
        {
            DeathLinkService.EnableDeathLink();
        }

        isInitializing = false;
        OnSessionConnected();
        return null;
    }

    private static void InitialiseSession(ArchipelagoSession session, DeathLinkService deathLinkService)
    {
        isInitializing = true;
        session.Socket.SocketClosed += _ => SimpleUI.connected = false;

        session.Items.ItemReceived += (helper) =>
        {
            var item = helper.PeekItem();
            if (item == null) return;

            var name = item.ItemName;
            if (name == Globals.PLUS_ENERGY_NAME)
                ArchipelagoModifiers.energyAdjustment++;
            else if (name == Globals.PLUS_CARDPLAYS_NAME)
                ArchipelagoModifiers.cardplaysAdjustment++;
            else if (name == Globals.PLUS_BLIGHT_NAME)
                ArchipelagoModifiers.blightAdjustment++;
            else if (Enum.TryParse<ElementType>(name, out var element))
            {
                if (!isInitializing && !ArchipelagoModifiers.ElementsAdjustment.TryAdd(element, 1))
                {
                    ArchipelagoModifiers.ElementsAdjustment[element]++;
                }
            }
            else
            {
                logger.LogInfo($"{name} added to allowed items");
                ArchipelagoModifiers.GottenItems[name] = ArchipelagoModifiers.GottenItems.GetValueOrDefault(name) + 1;

                if (HintCards >= HintCardsOption.Always)
                {
                    var nameIfCard = $"{Globals.PLAY_CARD_PREFIX}{name}";
                    var id = Session?.Locations.GetLocationIdFromName(Globals.GAME_NAME, nameIfCard);
                    if (id is long i && i != -1)
                        _ = Task.Run(() => ScoutService.Scout(i));
                }
            }

            helper.DequeueItem();
        };

        deathLinkService.OnDeathLinkReceived += (_) => { /* TODO handle death */ };
    }

    private static void OnSessionConnected()
    {
        while (queuedLocations.Count > 0)
        {
            var name = queuedLocations.Dequeue();
            LocationService.CheckLocation(name);
        }
    }

    public static void CardPlayed(string cardName)
    {
        logger.LogInfo($"{cardName} played");
        LocationService.CheckLocation($"{Globals.PLAY_CARD_PREFIX}{cardName}");
    }

    public static void AdversaryDefeated(string name, int difficulty, string spirit)
    {
        logger.LogInfo($"Defeated {name} with {spirit} on level {difficulty}");
        for (int i = 0; i <= difficulty; i++)
        {
            LocationService.CheckLocation($"Defeat {name} with {spirit} on level {i}");
            LocationService.CheckLocation($"Defeat {name} with {Globals.ANY_SPIRIT} on level {i}");
        }
    }

    public static void GameLost()
    {
        logger.LogInfo($"Player died{(Deathlink == 0 ? "" : ", sending deathlink")}");
        if (DeathLinkService == null || Session == null || Deathlink == 0) return;
        DeathLinkService.SendDeathLink(new DeathLink(Session.Players.ActivePlayer.Alias, "Failed to protect the island"));
    }

    public static IReadOnlyCollection<long> AllLocationsChecked() =>
        Session?.Locations.AllLocationsChecked ?? new List<long>().AsReadOnly();
}
