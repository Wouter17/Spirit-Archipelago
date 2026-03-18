using Archipelago.UI;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Logger = BepInEx.Logging.Logger;

namespace Archipelago;

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
