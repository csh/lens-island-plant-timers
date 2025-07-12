using TMPro;
using UnityEngine;

namespace PlantTimers.Tooltips;

public class DryPlotTooltip : MonoBehaviour
{
    private const float VerticalOffset = 1.75f;
    
    private GameObject _canvas;
    private TextMeshPro _tmp;
    private Camera _camera;
    private FarmPlot _plot;

    public void SetFarmPlot(FarmPlot plot)
    {
        _plot = plot;
    }

    private void Awake()
    {
        _camera = Camera.main;

        _canvas = new GameObject("DryPlotTooltipCanvas");
        _canvas.transform.SetParent(transform, false);
        _canvas.transform.localPosition = new Vector3(0, VerticalOffset, 0);

        var canvas = _canvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = _camera;
        canvas.sortingOrder = 99;

        var textGameObject = new GameObject("TooltipText");
        textGameObject.transform.SetParent(_canvas.transform, false);
        textGameObject.transform.localPosition = Vector3.zero;

        _tmp = textGameObject.AddComponent<TextMeshPro>();
        _tmp.text = "Needs Water!";
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.fontSize = 3.5f;
        _tmp.color = Color.cyan;
        _tmp.enableWordWrapping = false;
        _tmp.rectTransform.sizeDelta = new Vector2(12f, 3f);

        _canvas.SetActive(false);
    }

    private void OnBecameVisible()
    {
        _canvas.SetActive(_plot.isDry && _plot.HasGrowingPlants());
#if DEBUG
        PlantTimerPlugin.Logger.LogDebug("[DryPlotTooltip] Setting tooltip for plot to visible");
#endif
    }

    private void OnBecameInvisible()
    {
        _canvas.SetActive(false);
#if DEBUG
        PlantTimerPlugin.Logger.LogDebug("[DryPlotTooltip] Setting tooltip for plot to invisible");
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

    private void Update()
    {
        if (_canvas.activeSelf == false) return;

        if (_plot.isDry == false)
        {
            Hide();
        }
    }

    private void LateUpdate()
    {
        if (_canvas.activeSelf && _camera)
        {
            _canvas.transform.rotation =
                Quaternion.LookRotation(_canvas.transform.position - _camera.transform.position);
        }
    }

    private void OnDestroy()
    {
        if (_canvas)
        {
            Destroy(_canvas);
        }
    }
}