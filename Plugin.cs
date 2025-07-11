using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantTimers;

public static class PlantExtensions
{
    public static bool IsGrown(this Plant plant)
    {
        if (plant == null)
            return false;

        if (plant.fullGrown)
            return true;

        var renderers = plant.transform.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].name == "Plant_Sparkle")
                return true;
        }

        return false;
    }
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PlantTimerPlugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private static PlantTimerPlugin Instance => _instance;
    private static PlantTimerPlugin _instance;
    private static ManualLogSource Log => Instance.Logger;

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Log.LogInfo($"[{MyPluginInfo.PLUGIN_NAME}] Starting up");

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        Log.LogInfo($"[{MyPluginInfo.PLUGIN_NAME}] Patches applied");

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
        foreach (var tooltip in FindObjectsOfType<TooltipComponent>(true))
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

    private void InstrumentAllPlants()
    {
        foreach (var plant in FindObjectsOfType<Plant>(true))
        {
            AttachLogger(plant);
        }
    }

    private void AttachLogger(Plant plant)
    {
        Logger.LogInfo($"== Begin Attach ==");
        Logger.LogInfo($"\tPlant name: {plant.name}");
        Logger.LogInfo($"\tIs grown? {plant.IsGrown()}");
        Logger.LogInfo($"\tIs tree? {plant.isTree}");
        Logger.LogInfo($"\tIs dead? {plant.isDead}");

        try
        {
            if (plant.isDead)
            {
                Logger.LogWarning("Plant is dead, skipping attaching timer tooltip.");
                return;
            }

            if (plant.IsGrown())
            {
                Logger.LogWarning("Plant is grown, skipping attaching timer tooltip.");
                return;
            }

            var renderers = plant.transform.GetComponentsInChildren<Renderer>();
#if DEBUG

            Logger.LogInfo("\tRenderers:");
            foreach (var renderer in renderers)
            {
                Logger.LogInfo($"\t\t- {renderer.name}");
            }
#endif

            foreach (var renderer in renderers)
            {
                if (renderer.name.StartsWith("Plant_") && renderer.name != "Plant_Sparkle")
                {
                    var go = renderer.gameObject;
                    if (!go.GetComponent<TooltipComponent>())
                    {
                        var tooltip = go.AddComponent<TooltipComponent>();
                        tooltip.SetPlant(plant);
                        tooltip.Show();

                        Logger.LogInfo($"\tAdded TooltipComponent to: {go.name}");
                    }
                }
            }
        }
        finally
        {
            Logger.LogInfo($"== End Attach ==");
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

            // Use frustum planes for robust "is in view" check:
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
            {
                Logger.LogInfo($"[DIAG] Plant in view: {plant.name} ({plant.transform.position})");

                AttachLogger(plant);

                count++;
            }
        }
        Logger.LogInfo($"[DIAG] Total plants in camera view: {count}");
    }

    [HarmonyPatch]
    class CropNetworking_ActOnPlantCrop_Patch
    {
        static MethodBase TargetMethod()
            => AccessTools.Method("CropNetworking:ActOnPlantCrop");

        static void Postfix(ref PlantCropAction data, bool asOwner)
        {
            Instance?.Logger.LogError($"[PATCH] ActOnPlantCrop invoked. Planted: {data.plantName}, Zone: {data.data.zoneName}, AsOwner: {asOwner}");
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.SetGrowthStage))]
    class Plant_SetStage_Patch
    {
        static void Postfix(Plant __instance)
        {
            if (__instance.isDead || __instance.IsGrown())
            {
                foreach (var renderer in __instance.GetComponentsInChildren<Renderer>())
                {
                    foreach (var tooltip in renderer.GetComponents<TooltipComponent>())
                    {
                        Destroy(tooltip);
                    }
                }
                return;
            }

            Instance?.AttachLogger(__instance);
        }
    }


    #region Debug Mode
    [HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.DevBuild), MethodType.Getter)]
    class Patch_DevBuild
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false; // skip original
        }
    }

    // Patch DebugConsole.DevPlayingLiveBuild (static getter)
    [HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.DevPlayingLiveBuild), MethodType.Getter)]
    class Patch_DevPlayingLiveBuild
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false; // skip original
        }
    }

    // Patch DebugConsole.DebugCommandAllowed (static method)
    [HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.DebugCommandAllowed))]
    class Patch_DebugCommandAllowed
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false; // skip original
        }
    }
    #endregion

    public class TooltipComponent : MonoBehaviour
    {
        public string TooltipText = "Plant Tooltip";
        public float VerticalOffset = 1.5f;
        private GameObject _canvas;
        private TextMeshPro _tmp;
        private Transform _target;
        private Camera _camera;

        private Plant _plant;

        public void SetPlant(Plant plant)
        {
            _plant = plant;
        }

        void Awake()
        {
            _camera = Camera.main;
            _target = transform;
            _canvas = new GameObject("TooltipCanvas");
            _canvas.transform.SetParent(transform, false);
            _canvas.transform.localPosition = new Vector3(0, VerticalOffset, 0);

            var canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _camera;
            canvas.sortingOrder = 50;

            var textGO = new GameObject("TooltipText");
            textGO.transform.SetParent(_canvas.transform, false);
            textGO.transform.localPosition = Vector3.zero;

            _tmp = textGO.AddComponent<TextMeshPro>();
            _tmp.text = TooltipText;
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.fontSize = 1.65f;
            _tmp.color = Color.yellow;
            _tmp.enableWordWrapping = false;
            _tmp.rectTransform.sizeDelta = new Vector2(8f, 2f);

            _canvas.SetActive(false); // Start hidden
        }

        void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas);
            }
        }

        public void Show(string text = null)
        {
            if (text != null)
                _tmp.text = text;
            _canvas.SetActive(true);
        }

        public void Hide()
        {
            _canvas.SetActive(false);
        }

        void OnBecameVisible()
        {
            _canvas.SetActive(true);
#if DEBUG
            Instance.Logger.LogError($"[TooltipComponent] Setting tooltip for {_plant.name} to visible");
#endif
        }
        void OnBecameInvisible()
        {
            _canvas.SetActive(false);
#if DEBUG
            Instance.Logger.LogError($"[TooltipComponent] Setting tooltip for {_plant.name} to invisible");
#endif
        }

        void Update()
        {
            if (_canvas.activeSelf == false)
            {
                return;
            }

            if (_plant == null)
                return;

            if (_plant.isDead || _plant.IsGrown())
            {
                Destroy(this);
                return;
            }

            float deltaGameDays = (_plant.TimeUntilMaturity - TimeData.currentTime).timeInDays;
            float gameMinutes = deltaGameDays * 24f * 60f;
            float realSeconds = gameMinutes * 60f / TimeData.TimeFactor;

            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(realSeconds));
            int hours = totalSeconds / 3600;
            int minutes = totalSeconds % 3600 / 60;
            int seconds = totalSeconds % 60;

            string formatted;
            if (hours > 0)
                formatted = $"{hours}:{minutes:00}:{seconds:00}";
            else if (minutes > 0)
                formatted = $"{minutes}:{seconds:00}";
            else
                formatted = $"{seconds}s";

            Show(formatted);
        }

        void LateUpdate()
        {
            if (_tmp != null && _camera != null && _canvas.activeSelf)
            {
                // Make the tooltip face the camera
                _tmp.transform.parent.rotation = Quaternion.LookRotation(_camera.transform.forward);
            }
        }
    }
}