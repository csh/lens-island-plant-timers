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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ListVisiblePlants();
        }
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
            base.Logger.LogInfo($"Destroying {tooltip.name}");
            Destroy(tooltip);
        }

        foreach (var tooltip in FindObjectsOfType<DryPlotTooltipComponent>(true))
        {
            base.Logger.LogInfo($"Destroying {tooltip.name}");
            Destroy(tooltip);
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != "Game") return;
        DestroyAllTooltips();
    }

    private void InstrumentAllPlants()
    {
        foreach (var plant in FindObjectsOfType<Plant>(true))
        {
            AttachLogger(plant);
        }
    }

    internal void AttachLogger(Plant plant)
    {
        base.Logger.LogDebug($"== Begin Attach ==");
        base.Logger.LogDebug($"\tPlant name: {plant.name}");
        base.Logger.LogDebug($"\tIs grown? {plant.IsGrown()}");
        base.Logger.LogDebug($"\tIs tree? {plant.isTree}");
        base.Logger.LogDebug($"\tIs dead? {plant.isDead}");

        try
        {
            if (plant.isDead)
            {
                base.Logger.LogDebug("Plant is dead, skipping attaching timer tooltip.");
                return;
            }

            if (plant.IsGrown())
            {
                base.Logger.LogDebug("Plant is grown, skipping attaching timer tooltip.");
                return;
            }

            var renderers = plant.transform.GetComponentsInChildren<Renderer>();
#if DEBUG

            base.Logger.LogDebug("\tRenderers:");
            foreach (var renderer in renderers)
            {
                base.Logger.LogDebug($"\t\t- {renderer.name}");
            }
#endif

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

                        base.Logger.LogDebug($"\tAdded TooltipComponent to: {go.name}");
                    }
                }
            }
        }
        finally
        {
            base.Logger.LogDebug($"== End Attach ==");
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
                base.Logger.LogDebug($"[DIAG] Plant in view: {plant.name} ({plant.transform.position})");

                AttachLogger(plant);

                count++;
            }
        }

        base.Logger.LogDebug($"[DIAG] Total plants in camera view: {count}");
    }

    #region Debug Mode

    [HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.DevBuild), MethodType.Getter)]
    class Patch_DevBuild
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.DevPlayingLiveBuild), MethodType.Getter)]
    class Patch_DevPlayingLiveBuild
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.DebugCommandAllowed))]
    class Patch_DebugCommandAllowed
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    #endregion
}