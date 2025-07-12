using UnityEngine;

namespace PlantTimers;

public static class FarmingExtensions
{
    public static bool IsGrown(this Plant plant)
    {
        if (!plant)
            return false;

        if (plant.fullGrown)
            return true;

        var renderers = plant.transform.GetComponentsInChildren<Renderer>();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var renderer in renderers)
        {
            if (renderer.name == "Plant_Sparkle")
                return true;
        }

        return false;
    }

    public static bool HasGrowingPlants(this FarmPlot plot)
    {
        if (plot?.plantZones == null)
            return false;

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var zone in plot.plantZones)
        {
            var plant = zone?.plant;
            if (plant != null && !plant.isDead && !plant.IsGrown())
                return true;
        }
        return false;
    }
}