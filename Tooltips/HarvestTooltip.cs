using TMPro;
using UnityEngine;

namespace PlantTimers.Tooltips;

public class HarvestTooltip : MonoBehaviour
{
    private const string TooltipText = "Plant Tooltip";
    private const float VerticalOffset = 1.5f;
    
    private GameObject _canvas;
    private TextMeshPro _tmp;
    private Camera _camera;
    private Plant _plant;

    public void SetPlant(Plant plant)
    {
        _plant = plant;
        if (!_plant.farm)
        {
            PlantTimerPlugin.Logger.LogError("Parent FarmPlot not found");
        }
    }

    private void Awake()
    {
        _camera = Camera.main;
        _canvas = new GameObject("TooltipCanvas");

        _canvas.transform.SetParent(transform, false);
        _canvas.transform.localPosition = new Vector3(0, VerticalOffset, 0);

        var canvas = _canvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = _camera;
        canvas.sortingOrder = 50;

        var textGameObject = new GameObject("TooltipText");
        textGameObject.transform.SetParent(_canvas.transform, false);
        textGameObject.transform.localPosition = Vector3.zero;

        _tmp = textGameObject.AddComponent<TextMeshPro>();
        _tmp.text = TooltipText;
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.fontSize = 1.65f;
        _tmp.color = Color.yellow;
        _tmp.enableWordWrapping = false;
        _tmp.rectTransform.sizeDelta = new Vector2(8f, 2f);

        _canvas.SetActive(false);
    }

    private void OnDestroy()
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

    private void OnBecameVisible()
    {
        if (_plant.farm && _plant.farm.isDry)
        {
            _canvas.SetActive(false);
            return;
        }

        _canvas.SetActive(true);
#if DEBUG
        PlantTimerPlugin.Logger.LogDebug($"[HarvestTooltip] Setting tooltip for {_plant.name} to visible");
#endif
    }

    private void OnBecameInvisible()
    {
        _canvas.SetActive(false);
#if DEBUG
        PlantTimerPlugin.Logger.LogDebug($"[HarvestTooltip] Setting tooltip for {_plant.name} to invisible");
#endif
    }

    private void Update()
    {
        if (_canvas.activeSelf == false || !_plant)
        {
            return;
        }

        if (_plant.farm && _plant.farm.isDry)
        {
            _canvas.SetActive(false);
            return;
        }

        if (_plant.isDead || _plant.IsGrown())
        {
            Destroy(this);
            return;
        }

        var deltaGameDays = (_plant.TimeUntilMaturity - TimeData.currentTime).timeInDays;
        var gameMinutes = deltaGameDays * 24f * 60f;
        var realSeconds = gameMinutes * 60f / TimeData.TimeFactor;

        var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(realSeconds));
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;

        string formatted;
        if (hours > 0)
            formatted = $"{hours}:{minutes:00}:{seconds:00}";
        else if (minutes > 0)
            formatted = $"{minutes}:{seconds:00}";
        else
            formatted = $"{seconds}s";

        Show(formatted);
    }

    private void LateUpdate()
    {
        if (_tmp != null && _camera != null && _canvas.activeSelf)
        {
            _tmp.transform.parent.rotation = Quaternion.LookRotation(_camera.transform.forward);
        }
    }
}