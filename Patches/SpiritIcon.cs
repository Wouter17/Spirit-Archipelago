using System.Linq;
using Archipelago.Archipelago;
using Archipelago.UI;
using BepInEx.Logging;
using Handelabra.SpiritIsland.View;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logging.Logger;

namespace Archipelago.Patches;

[HarmonyPatch(typeof(NewGameSpiritItemView), nameof(NewGameSpiritItemView.Configure))]
class SpiritIconPatch
{
    static readonly ManualLogSource logger = Logger.CreateLogSource("SpiritIconPatch");
    static readonly string[] targets = ["Buy Button", "Requires Purchase Indicator"];

    static void Postfix(NewGameSpiritItemView __instance)
    {
        if (!ArchipelagoModifiers.LockedSpirits().Contains(__instance.SpiritIdentifier.ToLower()))
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
        img.sprite = ArchipelagoIcon.LoadIcon();
        var color = img.color;
        color.a = .75f;
        img.color = color;

        // Adjust position/size
        RectTransform rt = logoGO.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 10);
        rt.sizeDelta = new Vector2(100, 100);
        rt.localScale = Vector3.one;
    }
}
