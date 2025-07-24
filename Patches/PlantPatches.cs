using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using PlantTimers.Tooltips;
using UnityEngine;

namespace PlantTimers.Patches;

public static class PlantPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(Plant), nameof(Plant.OnEnable))]
    public static void OnEnablePostfix([SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")] Plant __instance)
    {
        if (__instance.isDead)
        {
            PlantTimerPlugin.Logger.LogDebug($"{__instance.name} is dead");
            return;
        }

        if (__instance.IsGrown())
        {
            PlantTimerPlugin.Logger.LogDebug($"{__instance.name} is grown");
            return;
        }

        if (__instance.gameObject.TryGetComponent<HarvestTooltip>(out _)) return;
        var tooltip = __instance.gameObject.AddComponent<HarvestTooltip>();
        var renderer = __instance.GetComponent<Renderer>();
        tooltip.SetPlant(__instance);
        if (renderer && renderer.isVisible && tooltip.ShouldBeVisible())
        {
            tooltip.Show();
        }
    }
    

    [HarmonyPostfix, HarmonyPatch(typeof(Plant), nameof(Plant.isDead), MethodType.Setter)]
    public static void SetIsDeadPostfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
        Plant __instance)
    {
        // Check incase the action was cancelled
        if (__instance.isDead == false) return;

        PlantTimerPlugin.Logger.LogDebug($"{__instance.name} was marked dead");
        if (__instance.TryGetComponent<HarvestTooltip>(out var tooltip))
        {
            Object.Destroy(tooltip.gameObject);
        }
    }
}