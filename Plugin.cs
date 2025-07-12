using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantTimers;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PlantTimerPlugin : BaseUnityPlugin
{
    private Harmony _harmony;
    internal static PlantTimerPlugin Instance => _instance;
    private static PlantTimerPlugin _instance;
    internal new static ManualLogSource Logger { get; private set; }

    private void Awake()
    {
        _instance = this;
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

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        DestroyAllTooltips();
        _harmony?.UnpatchSelf();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void DestroyAllTooltips()
    {
        foreach (var tooltip in FindObjectsOfType<TimerTooltip>(true))
        {
            Logger.LogInfo($"Destroying {tooltip.name}");
            Destroy(tooltip);
        }

        foreach (var tooltip in FindObjectsOfType<DryPlotTooltipComponent>(true))
        {
            Logger.LogInfo($"Destroying {tooltip.name}");
            Destroy(tooltip);
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != "Game") return;
        DestroyAllTooltips();
    }

    internal void AttachLogger(Plant plant)
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
                if (renderer.name.StartsWith("Plant_") && renderer.name != "Plant_Sparkle")
                {
                    var go = renderer.gameObject;
                    if (!go.GetComponent<TimerTooltip>())
                    {
                        var tooltip = go.AddComponent<TimerTooltip>();
                        tooltip.SetPlant(plant);
                        tooltip.Show();

                        Logger.LogDebug($"\tAdded TooltipComponent to: {go.name}");
                    }
                }
            }
        }
        finally
        {
            Logger.LogDebug($"== End Attach ==");
        }
    }

#if DEBUG

    private void InstrumentAllPlants()
    {
        foreach (var plant in FindObjectsOfType<Plant>(true))
        {
            AttachLogger(plant);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ListVisiblePlants();
        }
    }

    private void ListVisiblePlants()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        int count = 0;
        foreach (var plant in FindObjectsOfType<Plant>(true))
        {
            var renderer = plant.GetComponentInChildren<Renderer>();
            if (renderer == null)
                continue;

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
            {
                Logger.LogDebug($"[DIAG] Plant in view: {plant.name} ({plant.transform.position})");

                AttachLogger(plant);

                count++;
            }
        }

        Logger.LogDebug($"[DIAG] Total plants in camera view: {count}");
    }

#endif

}