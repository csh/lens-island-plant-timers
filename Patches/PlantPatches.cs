using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using PlantTimers.Tooltips;
using UnityEngine;

namespace PlantTimers.Patches;

public static class PlantPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(Plant), nameof(Plant.OnEnable))]
    public static void OnEnablePostfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")] Plant __instance)
    {
        if (__instance.isDead || __instance.IsGrown())
        {
            PlantTimerPlugin.Logger.LogDebug($"{__instance.name} is dead or fully grown");
            return;
        }
        
        foreach (var growthStage in __instance.growthStages)
        {
            if (growthStage.obj.name.Contains("Sparkle")) continue;
            if (growthStage.obj.TryGetComponent<HarvestTooltip>(out _)) continue;
            
            var tooltip = growthStage.obj.AddComponent<HarvestTooltip>();
            tooltip.SetPlant(__instance);
            if (tooltip.ShouldBeVisible())
            {
                tooltip.Show();
            }
        }
    }
    
    [HarmonyPostfix, HarmonyPatch(typeof(Plant), nameof(Plant.isDead), MethodType.Setter)]
    public static void SetIsDeadPostfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")] Plant __instance)
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