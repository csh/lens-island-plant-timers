using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using PlantTimers.Tooltips;
using UnityEngine;

namespace PlantTimers.Patches;

[HarmonyPatch(typeof(Plant), nameof(Plant.SetGrowthStage))]
public class PlantPatches
{
    [UsedImplicitly]
    public static void Postfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")] Plant __instance)
    {
        if (__instance.isDead || __instance.IsGrown())
        {
            foreach (var renderer in __instance.GetComponentsInChildren<Renderer>())
            {
                foreach (var tooltip in renderer.GetComponents<HarvestTooltip>())
                {
                    Object.Destroy(tooltip);
                }
            }

            return;
        }

        PlantTimerPlugin.AttachTooltips(__instance);
    }
}