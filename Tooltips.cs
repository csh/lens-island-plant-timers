using TMPro;
using UnityEngine;

namespace PlantTimers;

public class DryPlotTooltipComponent : MonoBehaviour
{
    public float VerticalOffset = 1.75f;
    private GameObject _canvas;
    private TextMeshPro _tmp;
    private Camera _camera;
    private FarmPlot _plot;

    public void SetFarmPlot(FarmPlot plot)
    {
        this._plot = plot;
    }

    void Awake()
    {
        _camera = Camera.main;

        _canvas = new GameObject("DryPlotTooltipCanvas");
        _canvas.transform.SetParent(transform, false);
        _canvas.transform.localPosition = new Vector3(0, VerticalOffset, 0);

        var canvas = _canvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = _camera;
        canvas.sortingOrder = 99;

        var textGO = new GameObject("TooltipText");
        textGO.transform.SetParent(_canvas.transform, false);
        textGO.transform.localPosition = Vector3.zero;

        _tmp = textGO.AddComponent<TextMeshPro>();
        _tmp.text = "Needs Water!";
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.fontSize = 3.5f;
        _tmp.color = Color.cyan;
        _tmp.enableWordWrapping = false;
        _tmp.rectTransform.sizeDelta = new Vector2(12f, 3f);

        _canvas.SetActive(false);
    }

    void OnBecameVisible()
    {
        _canvas.SetActive(true);
#if DEBUG
        PlantTimerPlugin.Logger.LogError($"[DryTooltipComponent] Setting tooltip for plot to visible");
#endif
    }

    void OnBecameInvisible()
    {
        _canvas.SetActive(false);
#if DEBUG
        PlantTimerPlugin.Logger.LogError($"[DryTooltipComponent] Setting tooltip for plot to invisible");
#endif
    }

    public void Show(string message = null)
    {
        if (_plot.isDry == false) return;

        if (message != null)
            _tmp.text = message;
        _canvas.SetActive(true);
    }

    public void Hide()
    {
        _canvas.SetActive(false);
    }

    void Update()
    {
        if (_canvas.activeSelf == false) return;

        if (_plot.isDry == false)
        {
            Hide();
        }
    }

    void LateUpdate()
    {
        if (_canvas.activeSelf && _camera != null)
        {
            _canvas.transform.rotation =
                Quaternion.LookRotation(_canvas.transform.position - _camera.transform.position);
        }
    }

    void OnDestroy()
    {
        if (_canvas != null)
            Destroy(_canvas);
    }
}

public class TimerTooltip : MonoBehaviour
{
    public string TooltipText = "Plant Tooltip";
    public float VerticalOffset = 1.5f;
    private FarmPlot _parentPlot;
    private GameObject _canvas;
    private TextMeshPro _tmp;
    private Camera _camera;
    private Plant _plant;

    public void SetPlant(Plant plant)
    {
        _plant = plant;
        _parentPlot = FindParentFarmPlot(_plant.transform);
        if (_parentPlot == null)
        {
            PlantTimerPlugin.Logger.LogError("Parent FarmPlot not found");
        }
    }

    private FarmPlot FindParentFarmPlot(Transform start)
    {
        var t = start;
        while (t != null)
        {
            var plot = t.GetComponent<FarmPlot>();
            if (plot != null) return plot;
            t = t.parent;
        }

        return null;
    }

    void Awake()
    {
        _camera = Camera.main;
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

        _canvas.SetActive(false);
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
        if (_parentPlot && _parentPlot.isDry)
        {
            _canvas.SetActive(false);
            return;
        }

        _canvas.SetActive(true);
#if DEBUG
        PlantTimerPlugin.Logger.LogError($"[TooltipComponent] Setting tooltip for {_plant.name} to visible");
#endif
    }

    void OnBecameInvisible()
    {
        _canvas.SetActive(false);
#if DEBUG
        PlantTimerPlugin.Logger.LogError($"[TooltipComponent] Setting tooltip for {_plant.name} to invisible");
#endif
    }

    void Update()
    {
        if (_canvas.activeSelf == false || _plant == null)
        {
            return;
        }

        if (_parentPlot && _parentPlot.isDry)
        {
            _canvas.SetActive(false);
            return;
        }

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
            _tmp.transform.parent.rotation = Quaternion.LookRotation(_camera.transform.forward);
        }
    }
}