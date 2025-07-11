using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace PlantTimers.Patches;

[HarmonyPatch(typeof(Plant), nameof(Plant.SetGrowthStage))]
internal class PlantPatches
{
    [UsedImplicitly]
    static void Postfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")] Plant __instance)
    {
        if (__instance.isDead || __instance.IsGrown())
        {
            foreach (var renderer in __instance.GetComponentsInChildren<Renderer>())
            {
                foreach (var tooltip in renderer.GetComponents<TimerTooltip>())
                {
                    Object.Destroy(tooltip);
                }
            }

            return;
        }

        PlantTimerPlugin.Instance?.AttachLogger(__instance);
    }
}