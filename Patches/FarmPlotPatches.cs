using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using PlantTimers.Tooltips;
using UnityEngine;

namespace PlantTimers.Patches;

[HarmonyPatch(typeof(FarmPlot), nameof(FarmPlot.Update))]
public class FarmPlotPatch
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class DryState
    {
        public bool WasDry;
    }
    
    private static readonly ConditionalWeakTable<FarmPlot, DryState> LastDryStates = [];

    [UsedImplicitly]
    public static void Postfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")] FarmPlot __instance)
    {
        var currentlyDry = __instance.isDry;
        if (!LastDryStates.TryGetValue(__instance, out var state))
        {
            LastDryStates.Add(__instance, new DryState { WasDry = !currentlyDry });
            return;
        }
        var wasDry = state.WasDry;

        switch (wasDry)
        {
            case false when currentlyDry:
                DisablePlantTimers(__instance);
                AddDryPlotTooltip(__instance);
                break;
            case true when !currentlyDry:
                HideDryPlotTooltip(__instance);
                RestorePlantTimers(__instance);
                break;
        }
        
        state.WasDry = currentlyDry;
    }

    private static void AddDryPlotTooltip(FarmPlot plot, string message = "Needs Water!")
    {
        if (!plot || !plot.HasGrowingPlants()) return;
        var root = plot.gameObject;
        var tip = root.GetComponent<DryPlotTooltip>() ??
                  root.AddComponent<DryPlotTooltip>();
        tip.SetFarmPlot(plot);
        tip.Show(message);
    }

    private static void HideDryPlotTooltip(FarmPlot plot)
    {
        if (!plot) return;
        var tip = plot.gameObject.GetComponent<DryPlotTooltip>();
        tip?.Hide();
    }

    private static void DisablePlantTimers(FarmPlot plot)
    {
        if (!plot || plot.plantZones == null) return;

        foreach (var zone in plot.plantZones)
        {
            if (!zone || !zone.plant) continue;

            var renderers = zone.plant.transform.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var tooltips = renderer.GetComponents<HarvestTooltip>();
                foreach (var tooltip in tooltips)
                {
                    tooltip.Hide();
                }
            }
        }
    }

    private static void RestorePlantTimers(FarmPlot plot)
    {
        if (!plot || plot.plantZones == null) return;

        foreach (var zone in plot.plantZones)
        {
            if (!zone || !zone.plant) continue;

            var renderers = zone.plant.transform.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var tooltips = renderer.GetComponents<HarvestTooltip>();
                foreach (var tooltip in tooltips)
                {
                    tooltip.Show();
                }
            }
        }
    }
}