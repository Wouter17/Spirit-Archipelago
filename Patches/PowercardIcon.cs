using System.Collections.Generic;
using Archipelago.Archipelago;
using Archipelago.Data;
using Archipelago.UI;
using Handelabra.SpiritIsland.View;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago.Patches;

[HarmonyPatch]
class PowercardIconPatch
{
    static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        var type = typeof(PowerCardView);
        yield return type.GetMethod(nameof(PowerCardView.Configure));
        yield return type.GetMethod(nameof(PowerCardView.UpdateDisplayForPlayPowerCard));
    }

    static void Postfix(PowerCardView __instance)
    {
        var go = (__instance as MonoBehaviour)?.gameObject;
        if (go == null) return;

        var locationId = APClient.Session?.Locations.GetLocationIdFromName(Globals.GAME_NAME, $"{Globals.PLAY_CARD_PREFIX}{__instance.Card.Title}");
        if (locationId == null || APClient.Session?.Locations.AllMissingLocations.Contains(locationId.Value) != true)
            return;

        GameObject logoGO = new GameObject("ArchipelagoLogo");

        // Parent to root
        logoGO.transform.SetParent(go.transform, false);

        // Add Image component
        Image img = logoGO.AddComponent<Image>();

        // Assign sprite
        img.sprite = ArchipelagoIcon.LoadIcon();
        var color = img.color;
        color.a = .75f;
        img.color = color;

        // Adjust position/size
        RectTransform rt = logoGO.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 10);
        rt.sizeDelta = new Vector2(50, 50);
        rt.localScale = Vector3.one;
    }
}
