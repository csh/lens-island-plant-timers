using System.Collections;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace PlantTimers.Tooltips;

public abstract class TooltipBase : MonoBehaviour
{
    protected static float VerticalOffset => 1.5f;

    protected GameObject Canvas;
    private TextMeshPro _label;
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

        Canvas = new GameObject("TooltipCanvas");
        Canvas.transform.SetParent(transform, false);
        Canvas.transform.localPosition = new Vector3(0, VerticalOffset, 0);

        var canvas = Canvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = _camera;
        canvas.sortingOrder = 50;
        
        var textObject = new GameObject("TooltipText");
        textObject.transform.SetParent(Canvas.transform, false);
        textObject.transform.localPosition = Vector3.zero;
        
        _label = textObject.AddComponent<TextMeshPro>();
        _label.alignment = TextAlignmentOptions.Center;
        _label.autoSizeTextContainer = false;
        _label.enableWordWrapping = false;
        _label.fontSize = 2f;
        _label.color = Color.cyan;
        _label.text = "";
        
        Canvas.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_label is not null && _camera && Canvas && Canvas.activeSelf)
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
        Canvas.SetActive(shouldBeVisible);
    }

    protected void OnBecameInvisible()
    {
        Hide();
    }
    
    private void OnDestroy()
    {
        StopUpdateLoop();
        
        if (!Canvas) return;
        
        Destroy(Canvas);
        Canvas = null;
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
        Canvas.SetActive(true);
    }

    public void Hide()
    {
        StopUpdateLoop();
        Canvas.SetActive(false);
    }
    
    [CanBeNull] protected abstract string GetTooltip();
    
    internal abstract bool ShouldBeVisible();
}