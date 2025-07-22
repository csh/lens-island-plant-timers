using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using PlantTimers.Tooltips;
using UnityEngine;

namespace PlantTimers.Patches;

public static class PlantPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(Plant), nameof(Plant.SetGrowthStage))]
    public static void SetGrowthStage(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
        Plant __instance)
    {
        if (__instance.isDead)
        {
            PlantTimerPlugin.Logger.LogDebug($"{__instance.name} is dead");
            return;
        }

        if (__instance.stageNum > 0)
        {
            var previousStage = __instance.growthStages[__instance.stageNum - 1];
            if (previousStage.obj && previousStage.obj.TryGetComponent<HarvestTooltip>(out var old))
            {
                old.Hide();
                Object.Destroy(old.gameObject);
            }
        }

        if (__instance.IsGrown()) return;
        if (__instance.currentStage.obj.TryGetComponent<HarvestTooltip>(out _)) return;
        
        var renderer = __instance.GetComponent<Renderer>();
        var tooltip = __instance.currentStage.obj.AddComponent<HarvestTooltip>();
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

        PlantTimerPlugin.Logger.LogDebug($"{__instance.name} is dead");
        foreach (var tooltip in __instance.GetComponentsInChildren<HarvestTooltip>(true))
        {
            Object.Destroy(tooltip.gameObject);
        }
    }
}