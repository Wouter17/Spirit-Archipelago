using System.Collections.Generic;
using System.Collections.ObjectModel;
using Archipelago.Archipelago;
using UnityEngine;

namespace Archipelago.UI;

public class SimpleUI : MonoBehaviour
{
    private Rect windowRect = new(20, 20, 230, 240);
    private bool showWindow = true;
    public static bool connected = false;
    private bool isConnecting = false;
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
                foreach (var goal in GoalService.goals)
                {
                    var completed = checkedLocations.Contains(goal);
                    if (!completed)
                    {
                        var name = APClient.Session?.Locations.GetLocationNameFromId(goal) ?? "Unable to fetch item name";
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

            if (GUILayout.Button("Connect") && !isConnecting)
            {
                ConnectAsync();
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

    async void ConnectAsync()
    {
        isConnecting = true;
        error = await APClient.Connect(room, slotName, password);

        if (error == null)
        {
            connected = true;
        }

        isConnecting = false;
    }

    public static void SetCheckedLocations(ReadOnlyCollection<long> set)
    {
        checkedLocations = set;
    }
}
