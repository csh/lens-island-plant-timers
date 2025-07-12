using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PlantTimers.Tooltips;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantTimers;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PlantTimerPlugin : BaseUnityPlugin
{
    private Harmony _harmony;
    internal new static ManualLogSource Logger { get; private set; }

    private void Awake()
    {
        Logger = base.Logger;
        DontDestroyOnLoad(gameObject);
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME}] Starting up");

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME}] Patches applied");

#if DEBUG

        if (SceneManager.GetActiveScene().name == "Game")
        {
            InstrumentAllPlants();
        }

#endif

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;
        InstrumentAllPlants();
    }

    private void OnDestroy()
    {
        DestroyAllTooltips();
        _harmony?.UnpatchSelf();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
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

    private static void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != "Game") return;
        DestroyAllTooltips();
    }

    internal static void AttachTooltips(Plant plant)
    {
        Logger.LogDebug($"== Begin Attach ==");
        Logger.LogDebug($"\tPlant name: {plant.name}");
        Logger.LogDebug($"\tIs grown? {plant.IsGrown()}");
        Logger.LogDebug($"\tIs tree? {plant.isTree}");
        Logger.LogDebug($"\tIs dead? {plant.isDead}");

        try
        {
            if (plant.isDead)
            {
                Logger.LogDebug("Plant is dead, skipping attaching timer tooltip.");
                return;
            }

            if (plant.IsGrown())
            {
                Logger.LogDebug("Plant is grown, skipping attaching timer tooltip.");
                return;
            }

            var renderers = plant.transform.GetComponentsInChildren<Renderer>();

            Logger.LogDebug("\tRenderers:");
            foreach (var renderer in renderers)
            {
                Logger.LogDebug($"\t\t- {renderer.name}");
            }

            foreach (var renderer in renderers)
            {
                if (!renderer.name.StartsWith("Plant_") || renderer.name == "Plant_Sparkle") continue;
                var go = renderer.gameObject;
                if (go.GetComponent<HarvestTooltip>()) continue;
                var tooltip = go.AddComponent<HarvestTooltip>();
                tooltip.SetPlant(plant);
                tooltip.Show();

                Logger.LogDebug($"\tAdded TooltipComponent to: {go.name}");
            }
        }
        finally
        {
            Logger.LogDebug($"== End Attach ==");
        }
    }

#if DEBUG

    private static void InstrumentAllPlants()
    {
        foreach (var plant in FindObjectsOfType<Plant>(true))
        {
            AttachTooltips(plant);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ListVisiblePlants();
        }
    }

    private static void ListVisiblePlants()
    {
        var camera = Camera.main;
        if (!camera)
        {
            return;
        }

        var count = 0;
        foreach (var plant in FindObjectsOfType<Plant>(true))
        {
            var renderer = plant.GetComponentInChildren<Renderer>();
            if (!renderer) continue;

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            if (!GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) continue;
            
            Logger.LogDebug($"[DIAG] Plant in view: {plant.name} ({plant.transform.position})");
            AttachTooltips(plant);

            count++;
        }

        Logger.LogDebug($"[DIAG] Total plants in camera view: {count}");
    }

#endif

}