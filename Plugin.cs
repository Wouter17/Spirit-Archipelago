using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using BepInEx;
using BepInEx.Logging;
using Handelabra.SpiritIsland.Engine;
using Handelabra.SpiritIsland.Engine.Controller;
using Handelabra.SpiritIsland.Engine.Model;
using Handelabra.SpiritIsland.View;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logging.Logger;

namespace Archipelago;

public static class Globals
{
    public const string PLUS_ENERGY_NAME = "+1 Energy";
    public const string PLUS_CARDPLAYS_NAME = "+1 Cardplay";
    public const string PLUS_BLIGHT_NAME = "+1 Blight";
    public const string PLAY_CARD_PREFIX = "Play: ";
    public const string GAME_NAME = "Spirit Island";
    public const string ANY_SPIRIT = "Any";
}

public static class ArchipelagoModifiers
{
    // public static HashSet<string> LockedCards = [];

    public static HashSet<string> BaseLockedCards { get; set; } = [];
    public static HashSet<string> BaseLockedSpirits { get; set; } = [];
    public static HashSet<string> BaseLockedAspects { get; set; } = [];
    public static HashSet<string> GottenItems { get; set; } = [];
    public static int energyAdjustment = 0;
    public static int cardplaysAdjustment = 0;
    public static int blightAdjustment = 0;

    public static HashSet<string> LockedCards()
    {
        var logger = Logger.CreateLogSource("ArchipelagoModifiers");
        return BaseLockedCards.Except(GottenItems).ToHashSet();
    }

    public static HashSet<string> LockedSpirits()
    {
        return BaseLockedSpirits.Except(GottenItems).Select(s => RemoveSpecial(s.ToLower())).ToHashSet();
    }

    public static HashSet<string> LockedAspects()
    {
        return BaseLockedAspects.Except(GottenItems).Select(a => RemoveSpecial(a.ToLower())).ToHashSet();
    }

    private static readonly Regex sWhitespace = new Regex(@"[^A-Za-z]");
    public static string RemoveSpecial(string input)
    {
        return sWhitespace.Replace(input, "");
    }
}

public static class ArchipelagoMessenger
{
    static readonly ManualLogSource logger = Logger.CreateLogSource("ArchipelagoMessenger");
    public static ArchipelagoSession? session;
    static DeathLinkService? deathLinkService;
    static readonly Queue<string> queuedLocations = new();
    static HashSet<long> goals = [-1];

    public static int Deathlink { get; private set; } = 0;

    public static void InitialiseSession(ref ArchipelagoSession session, ref DeathLinkService deathLinkService)
    {
        session.Socket.SocketClosed += (socket) =>
        {
            SimpleUI.connected = false;
        };

        session.Items.ItemReceived += (receivedItemsHelper) =>
        {
            var itemReceivedName = receivedItemsHelper.PeekItem();
            if (itemReceivedName == null) return;

            var itemName = itemReceivedName.ItemName;
            logger.LogInfo($"Received {itemName}");

            if (itemName == Globals.PLUS_ENERGY_NAME)
            {
                ArchipelagoModifiers.energyAdjustment++;
            }
            else if (itemName == Globals.PLUS_CARDPLAYS_NAME)
            {
                ArchipelagoModifiers.cardplaysAdjustment++;
            }
            else if (itemName == Globals.PLUS_BLIGHT_NAME)
            {
                ArchipelagoModifiers.blightAdjustment++;
            }
            else
            {
                logger.LogInfo($"Adding {itemName} to allowed items");
                ArchipelagoModifiers.GottenItems.Add(itemName);
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
        InitialiseSession(ref session, ref deathLinkService);

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
        logger.LogMessage($"Gotten items are {string.Join("", gottenItems)}");
        logger.LogMessage($"SlotData: {loginSuccess.SlotData.Join()}");

        ArchipelagoModifiers.energyAdjustment = Convert.ToInt32(loginSuccess.SlotData["base_energy_offset"]) + gottenItems.Count(name => name == Globals.PLUS_ENERGY_NAME);
        ArchipelagoModifiers.cardplaysAdjustment = Convert.ToInt32(loginSuccess.SlotData["base_cardplay_offset"]) + gottenItems.Count(name => name == Globals.PLUS_CARDPLAYS_NAME);
        ArchipelagoModifiers.blightAdjustment = Convert.ToInt32(loginSuccess.SlotData["base_blight_offset"]) + gottenItems.Count(name => name == Globals.PLUS_BLIGHT_NAME);
        ArchipelagoModifiers.BaseLockedCards = ((JArray)loginSuccess.SlotData["base_locked_cards"]).Values<string>().OfType<string>().ToHashSet();
        ArchipelagoModifiers.BaseLockedSpirits = ((JArray)loginSuccess.SlotData["base_locked_spirits"]).Values<string>().OfType<string>().ToHashSet();
        ArchipelagoModifiers.BaseLockedAspects = ((JArray)loginSuccess.SlotData["base_locked_aspects"]).Values<string>().OfType<string>().ToHashSet();

        goals = ((JArray)loginSuccess.SlotData["goals"]).Values<string>().ToList().Select(goal => session.Locations.GetLocationIdFromName(Globals.GAME_NAME, goal)).ToHashSet();

        Deathlink = Convert.ToInt32(loginSuccess.SlotData["deathlink"]);
        if (Deathlink == 1)
        {
            deathLinkService.EnableDeathLink();
        }
        return null;
    }

    public static void CardPlayed(string cardName)
    {
        logger.LogInfo($"{cardName} played");
        string locationName = $"{Globals.PLAY_CARD_PREFIX}{cardName}";
        CheckLocation(locationName);
    }

    public static void CheckLocation(string name)
    {
        logger.LogInfo($"Checking location: {name}");
        if (session?.Socket.Connected == true)
        {
            var id = session.Locations.GetLocationIdFromName(Globals.GAME_NAME, name);
            session.Locations.CompleteLocationChecks(id);
            CheckGoalCompletion();
        }
        else
        {
            queuedLocations.Append(name);
        }
    }

    public static void CheckGoalCompletion()
    {
        if (session != null)
        {
            SimpleUI.SetCheckedLocations(session.Locations.AllLocationsChecked);
            logger.LogInfo($"Checking for goal completion ::: goals: {string.Join(",", goals)}, checked locations: {string.Join(",", session.Locations.AllLocationsChecked)}");
        }
        if (session?.Socket.Connected == true && goals.IsSubsetOf(session.Locations.AllLocationsChecked))
        {
            session.SetGoalAchieved();
        }
    }

    public static void AdversairyDefeated(string AdversairyName, int difficulty, string spirit)
    {
        logger.LogInfo($"defeated {AdversairyName} with {spirit} on difficulty {difficulty}");
        for (var i = 0; i <= difficulty; i++)
        {
            string locationName = $"Defeat {AdversairyName} with {spirit} on difficulty {i}";
            CheckLocation(locationName);

            string anyLocationName = $"Defeat {AdversairyName} with {Globals.ANY_SPIRIT} on difficulty {i}";
            CheckLocation(anyLocationName);
        }
    }

    public static void GameLost()
    {
        logger.LogInfo($"Player died{(Deathlink == 0 ? "" : ", sending deathlink")}");
        if (deathLinkService == null || session == null || Deathlink == 0) return;
        deathLinkService.SendDeathLink(new DeathLink(session.Players.ActivePlayer.Alias, "Failed to protect the island"));
    }

    static void OnSessionConnected()
    {
        for (var i = 0; i < queuedLocations.Count; i++)
        {
            CheckLocation(queuedLocations.Dequeue());
        }
    }

    public static ReadOnlyCollection<long> AllLocationsChecked()
    {
        if (session == null)
        {
            return new List<long>().AsReadOnly();
        }
        return session.Locations.AllLocationsChecked;
    }

    public static HashSet<long> Goals()
    {
        if (session == null)
        {
            return [];
        }
        return goals;
    }
}

public class SimpleUI : MonoBehaviour
{
    private Rect windowRect = new(20, 20, 230, 240);
    private bool showWindow = true;
    public static bool connected = false;
    private string? error = null;

    private bool goalsOpen = false;
    private Vector2 scroll = Vector2.zero;
    string room = "archipelago.gg:";
    string slotName = "";
    string password = "";

    private static ReadOnlyCollection<long> checkedLocations = new List<long>().AsReadOnly();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            showWindow = !showWindow;
    }

    void OnGUI()
    {
        if (!showWindow) return;

        GUI.depth = 0;
        windowRect = GUILayout.Window(1, windowRect, DrawWindow, "Archipelago", GUILayout.MinHeight(120), GUILayout.MaxHeight(1000));
    }

    void DrawWindow(int id)
    {
        if (connected)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Connected");
            GUILayout.Label($"Energy offset: {ArchipelagoModifiers.energyAdjustment}");
            GUILayout.Label($"Cardplays offset: {ArchipelagoModifiers.cardplaysAdjustment}");
            GUILayout.Label($"Blight offset: {ArchipelagoModifiers.blightAdjustment}");

            goalsOpen = GUILayout.Toggle(goalsOpen, (goalsOpen ? "▼" : "▶") + " Remaining Goals", "Button");
            if (goalsOpen)
            {
                GUILayout.BeginVertical("box");
                foreach (var goal in ArchipelagoMessenger.Goals())
                {
                    var completed = checkedLocations.Contains(goal);
                    if (!completed)
                    {
                        var name = ArchipelagoMessenger.session?.Locations.GetLocationNameFromId(goal) ?? "Unable to fetch item name";
                        GUILayout.Label(name);
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
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

            if (GUILayout.Button("Exit"))
            {
                Plugin.Exit();
            }
        }

        if (error != null)
            GUILayout.Label(error);

        GUI.DragWindow();
    }

    public static void SetCheckedLocations(ReadOnlyCollection<long> set)
    {
        checkedLocations = set;
    }
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource? Logger;
    private static Harmony? harmony;

    private static SimpleUI? ui;

    private void Awake()
    {
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger = base.Logger;
        var DisableMod = Config.Bind("General", "DisableMod", false, "Temporarily disables the Archipelago mod without uninstalling.");
        if (DisableMod.Value)
        {
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is disabled!");
            return;
        }
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        harmony.PatchAll();
        ui = gameObject.AddComponent<SimpleUI>();
    }

    public static void Exit()
    {
        if (ui != null)
        {
            Destroy(ui);
        }
        harmony?.UnpatchSelf();
    }
}

[HarmonyPatch]
public class Power_activation_patch
{
    static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        var baseType = typeof(PowerController);

        foreach (var type in baseType.Assembly.GetTypes())
        {
            if (!baseType.IsAssignableFrom(type))
                continue;

            var method = AccessTools.Method(
                type,
                "UsePower",
                [typeof(BaseController), typeof(PowerUse)]
            );

            if (method != null)
                yield return method;
        }
    }
    static void Prefix(PowerController __instance)
    {
        ArchipelagoMessenger.CardPlayed(__instance.Title);
    }
}

[HarmonyPatch(typeof(GameController), nameof(GameController.GameOver))]
public class End_patch
{
    static readonly HashSet<EndingResult> wins = [EndingResult.WinSacrificeVictory, EndingResult.WinTerrorVictory, EndingResult.WinScenario, EndingResult.WinInvadersDestroyed];
    static void Prefix(GameController __instance, EndingResult endingResult)
    {
        var logger = Logger.CreateLogSource("end_patch");
        logger.LogMessage($"Game end");

        if (wins.Contains(endingResult))
        {
            logger.LogInfo("game won!");
            var spirits = __instance.SpiritControllers.Select(sc => sc.TitleWithAspect).ToList();
            logger.LogInfo($"Spirits that won are {string.Join(",", spirits)}");
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
            ArchipelagoMessenger.GameLost();
        }
    }
}

[HarmonyPatch(typeof(GameController), nameof(GameController.CheckForGameOver))]
public class Update_patch
{
    private static readonly ManualLogSource logger = Logger.CreateLogSource("Update_patch");
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

        var allowedMinorCards = __instance.OutOfGame.CardControllers.Where(card => card.IsMinorPowerCard && !ArchipelagoModifiers.LockedCards().Contains(card.Title));
        if (allowedMinorCards.Any())
        {
            MoveCardsAction moveCardsAction = __instance.MoveCards(null, MoveCardReason.Debugging, allowedMinorCards, __instance.MinorPowerDeck);
            var enumerator = __instance.DoAction(moveCardsAction, true, true);
            __instance.ExhaustCoroutine(enumerator);
            __instance.StartCoroutine(enumerator);
        }

        var allowedMajorCards = __instance.OutOfGame.CardControllers.Where(card => card.IsMajorPowerCard && !ArchipelagoModifiers.LockedCards().Contains(card.Title));
        if (allowedMajorCards.Any())
        {
            MoveCardsAction moveCardsAction = __instance.MoveCards(null, MoveCardReason.Debugging, allowedMajorCards, __instance.MajorPowerDeck);
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

[HarmonyPatch(typeof(Game), nameof(Game.CreateBlightCard), [typeof(string), typeof(IEnumerable<string>), typeof(bool)])]
public class Blight_patch
{
    static readonly ManualLogSource logger = Logger.CreateLogSource("Blight_patch");

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
            typeof(Blight_patch),
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

[HarmonyPatch(typeof(DLCHelper), nameof(DLCHelper.IsSpiritPlayable))]
public class Spirit_patch
{
    public static HashSet<string> notBought = [];
    static void Prefix(out string __state, string identifier)
    {
        __state = identifier;
    }
    static bool Postfix(bool __result, string __state)
    {
        if (!__result)
        {
            notBought.Add(__state);
        }
        return __result && !ArchipelagoModifiers.LockedSpirits().Contains(__state.ToLower());
    }
}

[HarmonyPatch(typeof(DLCHelper), nameof(DLCHelper.GetAllPlayableAspectsForSpirit))]
public class Aspect_patch
{
    static IEnumerable<string> Postfix(IEnumerable<string> __result)
    {
        return __result.Where(a => !ArchipelagoModifiers.LockedAspects().Contains(a.ToLower()));
    }
}

[HarmonyPatch(typeof(NewGameViewController), nameof(NewGameViewController.ShowPurchasePanel))]
public class Purchase_panel_patch
{
    static bool Prefix(SpiritController spirit)
    {
        if (Spirit_patch.notBought.Contains(spirit.Identifier))
        {
            return true;
        }
        return false;
    }
}

[HarmonyPatch(typeof(NewGameSpiritItemView), nameof(NewGameSpiritItemView.Configure))]
class NewGameSpiritItemView_patch
{
    static readonly ManualLogSource logger = Logger.CreateLogSource("NewGameSpiritItemView_patch");
    static readonly string[] targets = ["Buy Button", "Requires Purchase Indicator"];

    static void Postfix(NewGameSpiritItemView __instance)
    {
        if (Spirit_patch.notBought.Contains(__instance.SpiritIdentifier) || !ArchipelagoModifiers.LockedSpirits().Contains(__instance.SpiritIdentifier.ToLower()))
        {
            return;
        }

        // __instance is the NewGameSpiritItemView object
        var go = (__instance as MonoBehaviour)?.gameObject;
        if (go == null) return;


        foreach (Transform child in go.transform)
        {
            if (targets.Contains(child.gameObject.name))
            {
                var gc = child.gameObject;
                gc.SetActive(false);
            }
        }

        GameObject logoGO = new GameObject("ArchipelagoLogo");

        // Parent to root
        logoGO.transform.SetParent(go.transform, false);

        // Add Image component
        Image img = logoGO.AddComponent<Image>();

        // Assign sprite
        img.sprite = LoadLogo();
        var color = img.color;
        color.a = .75f;
        img.color = color;

        // Adjust position/size
        RectTransform rt = logoGO.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 10);
        rt.sizeDelta = new Vector2(100, 100);
        rt.localScale = Vector3.one;
    }

    public static Sprite? LoadLogo()
    {
        string path = Path.Combine(Paths.PluginPath, "./assets/archipelago_logo.png");

        if (!File.Exists(path))
        {
            logger.LogWarning($"Logo not found at {path}");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes); // load PNG into Texture2D

        // Create a sprite from the texture
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f) // pivot center
        );

        return sprite;
    }
}