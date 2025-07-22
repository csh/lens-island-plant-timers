using System.Linq;
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
    
    private Harmony _harmony;
    
    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_NAME}");

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(typeof(FarmPlotPatches));
        _harmony.PatchAll(typeof(PlantPatches));
        
        Logger.LogInfo("Patches applied");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu") return;
        DestroyAllTooltips();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
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