using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using PlantTimers.Tooltips;
using UnityEngine;

namespace PlantTimers.Patches;

public static class FarmPlotPatches
{
    private sealed class DryState
    {
        public bool WasDry;
    }

    private static readonly ConditionalWeakTable<FarmPlot, DryState> LastDryStates = [];

    [HarmonyPostfix, HarmonyPatch(typeof(FarmPlot), nameof(FarmPlot.OnEnable))]
    public static void OnEnablePostfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")] FarmPlot __instance)
    {
        var tooltip = __instance.gameObject.GetComponent<DryPlotTooltip>() ??
                      __instance.gameObject.AddComponent<DryPlotTooltip>();

        tooltip.SetFarmPlot(__instance);

        if (tooltip.ShouldBeVisible())
        {
            tooltip.Show();
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(FarmPlot), nameof(FarmPlot.Update))]
    public static void UpdatePostfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
        FarmPlot __instance)
    {
        if (__instance.HasGrowingPlants() == false)
        {
            if (__instance.gameObject.TryGetComponent<DryPlotTooltip>(out var tt))
            {
                tt.Hide();
            }
            return;
        }
        
        var currentlyDry = __instance.isDry;
        if (!LastDryStates.TryGetValue(__instance, out var state))
        {
            LastDryStates.Add(__instance, new DryState { WasDry = !currentlyDry });
            return;
        }

        var wasDry = state.WasDry;

        DryPlotTooltip tooltip;
        switch (wasDry)
        {
            case false when currentlyDry:
            {
                if (__instance.gameObject.TryGetComponent(out tooltip))
                {
                    tooltip.Show();
                }

                DisablePlantTimers(__instance);
                break;
            }
            case true when !currentlyDry:
            {
                if (__instance.gameObject.TryGetComponent(out tooltip))
                {
                    tooltip.Hide();
                }

                RestorePlantTimers(__instance);
                break;
            }
        }

        state.WasDry = currentlyDry;
    }

    private static void DisablePlantTimers(FarmPlot plot)
    {
        if (!plot || plot.plantZones == null) return;

        foreach (var zone in plot.plantZones)
        {
            if (!zone || !zone.plant || zone.plant.isDead || zone.plant.IsGrown()) continue;
            foreach (var tooltip in zone.plant.GetComponentsInChildren<HarvestTooltip>())
            {
                tooltip.Hide();
            }
        }
    }

    private static void RestorePlantTimers(FarmPlot plot)
    {
        if (!plot || plot.plantZones == null) return;

        foreach (var zone in plot.plantZones)
        {
            if (!zone || !zone.plant || zone.plant.isDead || zone.plant.IsGrown()) continue;
            foreach (var tooltip in zone.plant.GetComponentsInChildren<HarvestTooltip>())
            {
                tooltip.Show();
            }
        }
    }
}