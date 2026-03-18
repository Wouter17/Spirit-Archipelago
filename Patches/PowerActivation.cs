using System.Collections.Generic;
using Archipelago.Archipelago;
using Handelabra.SpiritIsland.Engine.Controller;
using HarmonyLib;

namespace Archipelago.Patches;

[HarmonyPatch]
public class PowerActivationPatch
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
        APClient.CardPlayed(__instance.Title);
    }
}
