using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace PlantTimers;

[HarmonyPatch(typeof(FarmPlot), nameof(FarmPlot.Update))]
internal class FarmPlotPatch
{
    private static readonly Dictionary<FarmPlot, bool> LastDryStates = [];

    [UsedImplicitly]
    static void Postfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")] FarmPlot __instance)
    {
        var currentlyDry = __instance.isDry;
        if (!LastDryStates.TryGetValue(__instance, out var wasDry))
            wasDry = !currentlyDry;

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

        LastDryStates[__instance] = currentlyDry;
    }

    private static void AddDryPlotTooltip(FarmPlot plot, string message = "Needs Water!")
    {
        if (plot == null || !plot.HasGrowingPlants()) return;
        var root = plot.gameObject;
        var tip = root.GetComponent<DryPlotTooltipComponent>() ??
                  root.AddComponent<DryPlotTooltipComponent>();
        tip.SetFarmPlot(plot);
        tip.Show(message);
    }

    private static void HideDryPlotTooltip(FarmPlot plot)
    {
        if (plot == null) return;
        var tip = plot.gameObject.GetComponent<DryPlotTooltipComponent>();
        tip?.Hide();
    }

    private static void DisablePlantTimers(FarmPlot plot)
    {
        if (plot == null || plot.plantZones == null) return;

        foreach (var zone in plot.plantZones)
        {
            if (zone == null || zone.plant == null) continue;

            var renderers = zone.plant.transform.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var tooltips = renderers[i].GetComponents<TimerTooltip>();
                for (int j = 0; j < tooltips.Length; j++)
                {
                    tooltips[j].Hide();
                }
            }
        }
    }

    private static void RestorePlantTimers(FarmPlot plot)
    {
        if (plot == null || plot.plantZones == null) return;

        foreach (var zone in plot.plantZones)
        {
            if (zone == null || zone.plant == null) continue;

            var renderers = zone.plant.transform.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var tooltips = renderers[i].GetComponents<TimerTooltip>();
                for (int j = 0; j < tooltips.Length; j++)
                {
                    tooltips[j].Show();
                }
            }
        }
    }
}