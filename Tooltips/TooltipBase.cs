using System.Collections;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace PlantTimers.Tooltips;

public abstract class TooltipBase : MonoBehaviour
{
    private const float VerticalOffset = 1.5f;
    
    private TextMeshPro _label;
    private GameObject _canvas;
    private Camera _camera;

    private Coroutine _updateLabel;

    public Color LabelColour
    {
        get => _label.color;
        set => _label.color = value;
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
        
        var textObject = new GameObject("TooltipText");
        textObject.transform.SetParent(_canvas.transform, false);
        textObject.transform.localPosition = Vector3.zero;
        
        _label = textObject.AddComponent<TextMeshPro>();
        _label.alignment = TextAlignmentOptions.Center;
        _label.autoSizeTextContainer = false;
        _label.enableWordWrapping = false;
        _label.fontSize = 2f;
        _label.color = Color.cyan;
        _label.text = "";
        
        _canvas.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_label is not null && _camera && _canvas && _canvas.activeSelf)
        {
            _label.transform.parent.rotation = Quaternion.LookRotation(_camera.transform.forward);
        }
    }
    
    protected void OnBecameVisible()
    {
        var shouldBeVisible = ShouldBeVisible();
        if (shouldBeVisible)
        {
            StartUpdateLoop();
        }
        _canvas.SetActive(shouldBeVisible);
    }

    protected void OnBecameInvisible()
    {
        Hide();
    }
    
    private void OnDestroy()
    {
        StopUpdateLoop();
        
        if (!_canvas) return;
        
        Destroy(_canvas);
        _canvas = null;
    }

    private void StartUpdateLoop()
    {
        if (_updateLabel is not null) return;

        _updateLabel = StartCoroutine(UpdateTooltipLoop());
    }

    private void StopUpdateLoop()
    {
        if (_updateLabel is null) return;
        
        StopCoroutine(_updateLabel);
        _updateLabel = null;
    }

    private IEnumerator UpdateTooltipLoop()
    {
        while (true)
        {
            var text = GetTooltip();
            if (text is not null)
            {
                _label.text = text;
            }
            else
            {
                Hide();
                yield break;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    public void Show()
    {
        StartUpdateLoop();
        _canvas.SetActive(true);
    }

    public void Hide()
    {
        StopUpdateLoop();
        _canvas.SetActive(false);
    }
    
    [CanBeNull] protected abstract string GetTooltip();
    
    internal abstract bool ShouldBeVisible();
}