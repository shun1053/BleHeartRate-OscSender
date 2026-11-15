using UnityEngine;
using UnityEngine.UI;

/// <summary>Checks dependent geometry after layout, before UI meshes are rebuilt.</summary>
public abstract class CanvasGeometryUpdater : MonoBehaviour, ICanvasElement
{
    private bool refreshRequested = true;

    protected virtual void OnEnable()
    {
        Canvas.preWillRenderCanvases += QueuePostLayoutCheck;
        Refresh();
    }

    protected virtual void OnDisable()
    {
        Canvas.preWillRenderCanvases -= QueuePostLayoutCheck;
        CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
    }

    protected virtual void OnValidate()
    {
        refreshRequested = true;
    }

    protected virtual void OnRectTransformDimensionsChange()
    {
        refreshRequested = true;
    }

    public void Refresh()
    {
        refreshRequested = true;
    }

    private void QueuePostLayoutCheck()
    {
        if (isActiveAndEnabled)
            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
    }

    public void Rebuild(CanvasUpdate executing)
    {
        if (executing != CanvasUpdate.PostLayout || !isActiveAndEnabled) return;
        bool forceRefresh = refreshRequested;
        refreshRequested = false;
        UpdateGeometryIfChanged(forceRefresh);
    }

    protected abstract void UpdateGeometryIfChanged(bool forceRefresh);

    public void LayoutComplete() { }
    public void GraphicUpdateComplete() { }
    public bool IsDestroyed() => this == null;
}
