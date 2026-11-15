using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Connects the scene controls to BLE state and optimizes its canvases.</summary>
public sealed class BleApplicationView : MonoBehaviour
{
    private HeartRateObserver observer;
    private AppSettingsHandler settings;
    private Toggle heartRateOnlyToggle;
    private TMP_Text scanStatus;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var candidate in FindObjectsOfType<HeartRateObserver>())
        {
            if (candidate.gameObject.scene == scene && !candidate.GetComponent<BleApplicationView>())
                candidate.gameObject.AddComponent<BleApplicationView>();
        }
    }

    private void Awake()
    {
        observer = GetComponent<HeartRateObserver>();
        settings = FindObjectOfType<AppSettingsHandler>();
        if (settings)
        {
            observer.onReturnToDeviceList.AddListener(settings.ShowDeviceList);
            observer.onDeviceSelected.AddListener(settings.ShowConnectionView);
        }
        observer.onStatusChanged.AddListener(SetScanStatus);
        observer.onConnectionFailed.AddListener(SetScanStatus);
        observer.onBusyChanged.AddListener(SetBusy);
        BindHeartRateFilter();
        AddScanStatus();
        OptimizeCanvases();
    }

    private void BindHeartRateFilter()
    {
        if (!observer.deviceScanButtonText) return;
        var root = observer.deviceScanButtonText.canvas;
        if (!root) return;
        foreach (var toggle in root.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle.transform.parent.name != "HRServiceOnlyToggleArea") continue;
            heartRateOnlyToggle = toggle;
            heartRateOnlyToggle.SetIsOnWithoutNotify(!observer.ShowAllDevices);
            heartRateOnlyToggle.interactable = !observer.IsBusy;
            heartRateOnlyToggle.onValueChanged.AddListener(SetHeartRateOnly);
            break;
        }
    }

    private void AddScanStatus()
    {
        if (!observer.deviceScanButtonText) return;
        var scanButton = observer.deviceScanButtonText.GetComponentInParent<Button>(true);
        if (!scanButton) return;
        var listRoot = scanButton.transform.parent.parent;
        var statusObject = new GameObject("ScanStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        statusObject.layer = listRoot.gameObject.layer;
        statusObject.transform.SetParent(listRoot, false);
        scanStatus = statusObject.GetComponent<TMP_Text>();
        scanStatus.font = observer.deviceScanButtonText.font;
        scanStatus.fontSize = 18;
        scanStatus.color = Color.white;
        scanStatus.alignment = TextAlignmentOptions.MidlineLeft;
        scanStatus.enableWordWrapping = true;
        scanStatus.raycastTarget = false;
        var rect = scanStatus.rectTransform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(12, -120);
        rect.offsetMax = new Vector2(-12, -78);
        SetScanStatus("Turn off the heart rate filter if your device is missing.");

        var scroll = listRoot.GetComponentInChildren<ScrollRect>(true);
        if (scroll)
        {
            var scrollRect = (RectTransform)scroll.transform;
            var max = scrollRect.offsetMax;
            max.y = -125;
            scrollRect.offsetMax = max;
        }
    }

    private void SetHeartRateOnly(bool heartRateOnly)
    {
        observer.SetShowAllDevices(!heartRateOnly);
    }

    private void SetBusy(bool busy)
    {
        if (heartRateOnlyToggle) heartRateOnlyToggle.interactable = !observer.IsBusy;
    }

    private void SetScanStatus(string message)
    {
        if (scanStatus && scanStatus.text != message) scanStatus.text = message;
    }

    private void OptimizeCanvases()
    {
        if (!observer.deviceScanButtonText) return;
        var root = observer.deviceScanButtonText.canvas;
        if (!root || root.renderMode != RenderMode.ScreenSpaceOverlay) return;
        var graph = FindObjectOfType<HeartRateGraphController>();
        if (graph && graph.heartrateAnimator)
        {
            var graphCanvas = graph.heartrateAnimator.GetComponent<Canvas>();
            if (!graphCanvas) graphCanvas = graph.heartrateAnimator.gameObject.AddComponent<Canvas>();
            graphCanvas.overrideSorting = false;
            graphCanvas.additionalShaderChannels = root.additionalShaderChannels;
        }
        foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            bool interactive = rect.name == "DeviceList";
            if (!interactive && rect.name != "Graph_Coodinate" && rect.name != "ConnectStatusArea") continue;
            var canvas = rect.GetComponent<Canvas>();
            if (!canvas) canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = false;
            canvas.additionalShaderChannels = root.additionalShaderChannels;
            if (interactive && !rect.GetComponent<GraphicRaycaster>()) rect.gameObject.AddComponent<GraphicRaycaster>();
        }
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            // Preserve scroll viewport hit surfaces, selectable target graphics and editable text.
            if (graphic.GetComponent<Mask>() || graphic.GetComponent<RectMask2D>() ||
                graphic.GetComponent<ScrollRect>()) continue;
            var selectable = graphic.GetComponentInParent<Selectable>(true);
            if (selectable && !(selectable is Button && graphic is TMP_Text)) continue;
            graphic.raycastTarget = false;
        }
    }

    private void OnDestroy()
    {
        if (heartRateOnlyToggle) heartRateOnlyToggle.onValueChanged.RemoveListener(SetHeartRateOnly);
        if (!observer) return;
        observer.onStatusChanged.RemoveListener(SetScanStatus);
        observer.onConnectionFailed.RemoveListener(SetScanStatus);
        observer.onBusyChanged.RemoveListener(SetBusy);
        if (settings)
        {
            observer.onReturnToDeviceList.RemoveListener(settings.ShowDeviceList);
            observer.onDeviceSelected.RemoveListener(settings.ShowConnectionView);
        }
    }
}
