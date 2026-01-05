using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using BepInEx;
using BepInEx.Logging;
using Handelabra.SpiritIsland.Engine;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;

namespace Archipelago;

public static class Globals
{
    public const string PLUS_ENERGY_NAME = "+1 Energy";
    public const string PLUS_CARDPLAYS_NAME = "+1 Cardplay";
    public const string GAME_NAME = "Spirit Island";
}

public static class ArchipelagoModifiers
{
    // cards to filter out, updated by the Archipelago network
    public static HashSet<string> LockedCards = [];
    public static int energyAdjustment = 0;
    public static int cardplaysAdjustment = 0;
}

public static class ArchipelagoMessenger
{
    static ManualLogSource logger = Logger.CreateLogSource("ArchipelagoMessager");
    static ArchipelagoSession? session;
    static DeathLinkService? deathLinkService;
    static Queue<string> queuedLocations = new();
    static HashSet<long> goals = new() { -1 };

    public static void initialiseSession(ref ArchipelagoSession session, ref DeathLinkService deathLinkService)
    {
        session.Socket.SocketClosed += (socket) =>
        {
            SimpleUI.connected = false;
        };

        session.Items.ItemReceived += (receivedItemsHelper) =>
        {
            var itemReceivedName = receivedItemsHelper.PeekItem();
            if (itemReceivedName == null) return;

            var itemName = itemReceivedName.ToString();

            if (itemName == Globals.PLUS_ENERGY_NAME)
            {
                ArchipelagoModifiers.energyAdjustment++;
            }
            else if (itemName == Globals.PLUS_CARDPLAYS_NAME)
            {
                ArchipelagoModifiers.cardplaysAdjustment++;
            }
            else
            {
                ArchipelagoModifiers.LockedCards.Remove(itemName);
            }

            receivedItemsHelper.DequeueItem();
        };

        deathLinkService.OnDeathLinkReceived += (deathLinkObject) =>
        {
            // TODO ... Kill your player(s).
        };

    }

    public static string? Connect(string server, string user, string password)
    {
        session = ArchipelagoSessionFactory.CreateSession(server);
        deathLinkService = session.CreateDeathLinkService();
        initialiseSession(ref session, ref deathLinkService);

        LoginResult result;

        try
        {
            // handle TryConnectAndLogin attempt here and save the returned object to `result`
            result = session.TryConnectAndLogin(Globals.GAME_NAME, user, ItemsHandlingFlags.AllItems, password: password);
        }
        catch (Exception e)
        {
            result = new LoginFailure(e.GetBaseException().Message);
        }

        if (!result.Successful)
        {
            LoginFailure failure = (LoginFailure)result;
            string errorMessage = $"Failed to Connect to {server} as {user}: ";
            foreach (string error in failure.Errors)
            {
                errorMessage += $"{error}\n    ";
            }
            foreach (ConnectionRefusedError error in failure.ErrorCodes)
            {
                errorMessage += $"{error}\n    ";
            }

            return errorMessage; // Did not connect, show the user the contents of `errorMessage`
        }

        // Successfully connected, `ArchipelagoSession` (assume statically defined as `session` from now on) can now be
        // used to interact with the server and the returned `LoginSuccessful` contains some useful information about the
        // initial connection (e.g. a copy of the slot data as `loginSuccess.SlotData`)
        var loginSuccess = (LoginSuccessful)result;
        OnSessionConnected();

        var gottenItems = session.Items.AllItemsReceived.Select(item => item.ItemName);
        ArchipelagoModifiers.energyAdjustment = (int)loginSuccess.SlotData["base_energy_offset"] + gottenItems.Where(name => name == Globals.PLUS_ENERGY_NAME).Count(); //TODO add gotten checks
        ArchipelagoModifiers.cardplaysAdjustment = (int)loginSuccess.SlotData["base_cardplay_offset"] + gottenItems.Where(name => name == Globals.PLUS_CARDPLAYS_NAME).Count(); //TODO add gotten checks
        var LockedCards = ((string)loginSuccess.SlotData["base_locekd_cards"]).Split(",").ToHashSet();
        LockedCards.RemoveWhere(name => gottenItems.Contains(name));
        ArchipelagoModifiers.LockedCards = LockedCards;
        goals = ((string)loginSuccess.SlotData["goals"]).Split(",").Select(goal => session.Locations.GetLocationIdFromName(Globals.GAME_NAME, goal)).ToHashSet();

        if ((bool)loginSuccess.SlotData["deathlink"])
        {
            deathLinkService.EnableDeathLink();
        }
        return null;
    }

    public static void cardPlayed(string cardName)
    {
        logger.LogMessage($"{cardName} played");
        string locationName = $"Play: {cardName}";
        checkLocation(locationName);
    }

    public static void checkLocation(string name)
    {
        if (session?.Socket.Connected == true)
        {
            var id = session.Locations.GetLocationIdFromName(Globals.GAME_NAME, name);
            session.Locations.CompleteLocationChecks(id);
            checkGoalCompletion();
        }
        else
        {
            queuedLocations.Append(name);
        }
    }

    public static void checkGoalCompletion()
    {
        if (session?.Socket.Connected == true && goals.IsSubsetOf(session.Locations.AllLocationsChecked))
        {
            session.SetGoalAchieved();
        }
    }

    public static void AdversairyDefeated(string AdversairyName, int difficulty, string spirit)
    {
        logger.LogMessage($"defeated {AdversairyName} on difficulty {difficulty} with {spirit}");
        for (var i = 0; i <= difficulty; i++)
        {
            string locationName = $"Defeat {AdversairyName} on difficulty {difficulty} with {spirit}";
            checkLocation(locationName);
        }
    }

    public static void gameLost()
    {
        logger.LogMessage($"Player died, sending deathlink");
        if (deathLinkService == null || session == null) return;
        deathLinkService.SendDeathLink(new DeathLink(session.Players.ActivePlayer.Alias, "Failed to protect the island"));
    }

    static void OnSessionConnected()
    {
        for (var i = 0; i < queuedLocations.Count; i++)
        {
            checkLocation(queuedLocations.Dequeue());
        }
    }
}

public class SimpleUI : MonoBehaviour
{
    private Rect windowRect = new Rect(20, 20, 250, 240);
    private bool showWindow = true;
    public static bool connected = false;
    private string? error = null;

    string room = "archipelago.gg:";
    string slotName = "";
    string password = "";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            showWindow = !showWindow;
    }

    void OnGUI()
    {
        if (!showWindow) return;

        // Make UI scale-independent
        GUI.depth = 0;
        windowRect = GUI.Window(12345, windowRect, DrawWindow, "Archipelago");
    }

    void DrawWindow(int id)
    {
        if (connected)
        {
            GUILayout.Label("Connected");
            GUILayout.Label($"Energy offset: {ArchipelagoModifiers.energyAdjustment}");
            GUILayout.Label($"Cardplays offset: {ArchipelagoModifiers.cardplaysAdjustment}");
        }
        else
        {
            GUILayout.Label("Room");
            room = GUILayout.TextField(room);

            GUILayout.Label("Slot name");
            slotName = GUILayout.TextField(slotName);

            GUILayout.Label("Password");
            password = GUILayout.PasswordField(password, '*');

            if (GUILayout.Button("Connect"))
            {
                error = ArchipelagoMessenger.Connect(room, slotName, password);
                if (error == null)
                {
                    connected = true;
                }
            }
        }

        if (error != null)
            GUILayout.Label(error);

        GUI.DragWindow();
    }
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource? Logger;

    private void Awake()
    {
        // Plugin startup logic
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        harmony.PatchAll();
        gameObject.AddComponent<SimpleUI>();
    }
}

[HarmonyPatch(typeof(PowerController), nameof(PowerController.UsePower))]
public class power_activation_patch
{
    static void Prefix(PowerController __instance)
    {
        ArchipelagoMessenger.cardPlayed(__instance.Title);
    }
}

[HarmonyPatch(typeof(GameController), nameof(GameController.GameOver))]
public class End_patch
{
    static readonly HashSet<EndingResult> wins = new() { EndingResult.WinSacrificeVictory, EndingResult.WinTerrorVictory, EndingResult.WinScenario, EndingResult.WinInvadersDestroyed };

    static void Prefix(GameController __instance, EndingResult endingResult)
    {
        var logger = Logger.CreateLogSource("end_patch");
        logger.LogMessage($"Game end");

        if (wins.Contains(endingResult))
        {
            logger.LogMessage("game won!");
            var spirits = __instance.SpiritControllers.Select(sc => sc.TitleWithAspect).ToList();
            logger.LogMessage($"Spirits that won are {string.Join(",", spirits)}");
            foreach (var adversary in __instance.AdversaryControllers)
            {
                foreach (var spirit in spirits)
                {
                    ArchipelagoMessenger.AdversairyDefeated(adversary.Title, adversary.Adversary.Level.GetValueOrDefault(), spirit);
                }
            }
        }
        else
        {
            ArchipelagoMessenger.gameLost();
        }
    }
}

[HarmonyPatch(typeof(GameController), nameof(GameController.CheckForGameOver))]
public class Update_patch
{
    static void Postfix(GameController __instance)
    {
        var allowedMinorCards = __instance.OutOfGame.CardControllers.Where(card => card.IsMinorPowerCard && !ArchipelagoModifiers.LockedCards.Contains(card.Title));
        if (allowedMinorCards.Any())
        {
            MoveCardsAction moveCardsAction = __instance.MoveCards(null, MoveCardReason.Debugging, allowedMinorCards, __instance.MinorPowerDeck);
            var enumerator = __instance.DoAction(moveCardsAction, true, true);
            __instance.ExhaustCoroutine(enumerator);
            __instance.StartCoroutine(enumerator);
        }

        var allowedMajorCards = __instance.OutOfGame.CardControllers.Where(card => card.IsMajorPowerCard && !ArchipelagoModifiers.LockedCards.Contains(card.Title));
        if (allowedMajorCards.Any())
        {
            MoveCardsAction moveCardsAction = __instance.MoveCards(null, MoveCardReason.Debugging, allowedMajorCards, __instance.MajorPowerDeck);
            var enumerator = __instance.DoAction(moveCardsAction, true, true);
            __instance.ExhaustCoroutine(enumerator);
            __instance.StartCoroutine(enumerator);
        }
    }
}

[HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
public class Shuffle_patch
{

    [HarmonyPostfix]
    static void Postfix(GameController __instance)
    {
        var logger = Logger.CreateLogSource("shuffle_patch");
        logger.LogMessage($"moving cards");
        var allowedCards = __instance.MajorPowerDeck.CardControllers.Concat(__instance.MinorPowerDeck.CardControllers).Where(card => !ArchipelagoModifiers.LockedCards.Contains(card.Title));
        MoveCardsAction moveCardsAction = __instance.MoveCards(null, MoveCardReason.Debugging, allowedCards, __instance.OutOfGame);
        var enumerator = __instance.DoAction(moveCardsAction, true, true);
        __instance.ExhaustCoroutine(enumerator);
        __instance.StartCoroutine(enumerator);
    }

}

[HarmonyPatch(typeof(SpiritController), nameof(SpiritController.GetEnergyPerTurn))]
public class Energy_patch
{
    [HarmonyPostfix]
    static int Postfix(int energy)
    {
        return Math.Max(0, energy + ArchipelagoModifiers.energyAdjustment);
    }
}

[HarmonyPatch(typeof(SpiritController), nameof(SpiritController.GetCardPlaysPerTurn))]
public class Cardplays_patch
{
    [HarmonyPostfix]
    static int Postfix(int cardplays)
    {
        return Math.Max(1, cardplays + ArchipelagoModifiers.cardplaysAdjustment);
    }
}