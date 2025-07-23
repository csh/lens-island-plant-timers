using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PlantTimers.Patches;
using PlantTimers.Tooltips;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantTimers;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PlantTimerPlugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    internal static Color DryLabelColour = Color.red;
    internal static Color HarvestLabelColour = Color.white;
    private ConfigEntry<string> _harvestLabelColour;
    private ConfigEntry<string> _dryLabelColour;
    
    private Harmony _harmony;
    
    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_NAME}");

        _harvestLabelColour = Config.Bind("Colour", "Harvest Timer", "1, 1, 1, 1");
        _harvestLabelColour.SettingChanged += UpdateHarvestLabelColour;
        HarvestLabelColour = ParseColour(_harvestLabelColour.Value) ?? Color.white;
        
        _dryLabelColour = Config.Bind("Colour", "Watering Reminder", "0.95, 0.35, 0.45, 1.0");
        _dryLabelColour.SettingChanged += UpdateDryLabelColour;
        DryLabelColour = ParseColour(_harvestLabelColour.Value) ?? new Color(0.95f, 0.35f, 0.45f, 1.0f);
        
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(typeof(FarmPlotPatches));
        _harmony.PatchAll(typeof(PlantPatches));
        
        Logger.LogInfo("Patches applied");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static Color? ParseColour(string colour)
    {
        try
        {
            var values = colour.Split(',');
            if (values.Length != 3 && values.Length != 4) return null;
        
            if (!float.TryParse(values[0], out var r) ||
                !float.TryParse(values[1], out var g) ||
                !float.TryParse(values[2], out var b))
            {
                return null;
            }

            var a = 1.0f;
            if (values.Length == 4 && !float.TryParse(values[3], out a))
            {
                return null;
            }
        
            return new Color(r, g, b, a);
        }
        catch
        {
            return null;
        }
    }
    
    private static void UpdateExistingLabels<T>(ConfigEntry<string> configEntry, ref Color targetColor) where T : TooltipBase
    {
        var color = ParseColour(configEntry.Value);
        if (!color.HasValue)
        {
            Logger.LogError($"Failed to parse colour for \"{configEntry.Definition.Section}/{configEntry.Definition.Key}\"");
            return;
        }
    
        targetColor = color.Value;
        foreach (var tooltip in FindObjectsOfType<T>(true))
        {
            tooltip.LabelColour = color.Value;
        }
    }

    private void UpdateHarvestLabelColour(object sender, EventArgs e)
    {
        UpdateExistingLabels<HarvestTooltip>(_harvestLabelColour, ref HarvestLabelColour);
    }

    private void UpdateDryLabelColour(object sender, EventArgs e)
    {
        UpdateExistingLabels<DryPlotTooltip>(_dryLabelColour, ref DryLabelColour);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            DestroyAllTooltips();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_harvestLabelColour is not null)
        {
            _harvestLabelColour.SettingChanged -= UpdateHarvestLabelColour;
        }

        if (_dryLabelColour is not null)
        {
            _dryLabelColour.SettingChanged -= UpdateDryLabelColour;
        }
        
        DestroyAllTooltips();
        _harmony?.UnpatchSelf();
    }

    private static void DestroyAllTooltips()
    {   
        foreach (var tooltip in FindObjectsOfType<HarvestTooltip>(true))
        {
            Logger.LogDebug($"Destroying {tooltip.name}");
            Destroy(tooltip);
        }

        foreach (var tooltip in FindObjectsOfType<DryPlotTooltip>(true))
        {
            Logger.LogDebug($"Destroying {tooltip.name}");
            Destroy(tooltip);
        }
    }
}