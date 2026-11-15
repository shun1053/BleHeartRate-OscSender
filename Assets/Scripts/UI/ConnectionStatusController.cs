using UnityEngine;
using TMPro;

public class ConnectionStatusController : MonoBehaviour
{
    public HeartRateObserver heartRateObserver;
    public GameObject loadingImageObject;
    public GameObject connectedImageObject;
    public TMP_Text statusText;
    int lastHeartRate = -1;

    void Start()
    {
        if (heartRateObserver == null) return;
        heartRateObserver.onDeviceSelected.AddListener(OnDeviceSelected);
        heartRateObserver.onServiceFound.AddListener(OnServiceFound);
        heartRateObserver.onCharacteristicFound.AddListener(OnCharacteristicFound);
        heartRateObserver.onSubscribed.AddListener(OnSubscribed);
        heartRateObserver.onHeartRateReceived.AddListener(OnHeartRateReceived);
        heartRateObserver.onDeviceTimeout.AddListener(OnTimeout);
        heartRateObserver.onDeviceReconnectFailed.AddListener(OnReconnectFailed);
        heartRateObserver.onStatusChanged.AddListener(SetStatusText);
        heartRateObserver.onBusyChanged.AddListener(OnBusyChanged);
        RefreshIcons();
    }

    void OnDestroy()
    {
        if (heartRateObserver == null) return;
        heartRateObserver.onDeviceSelected.RemoveListener(OnDeviceSelected);
        heartRateObserver.onServiceFound.RemoveListener(OnServiceFound);
        heartRateObserver.onCharacteristicFound.RemoveListener(OnCharacteristicFound);
        heartRateObserver.onSubscribed.RemoveListener(OnSubscribed);
        heartRateObserver.onHeartRateReceived.RemoveListener(OnHeartRateReceived);
        heartRateObserver.onDeviceTimeout.RemoveListener(OnTimeout);
        heartRateObserver.onDeviceReconnectFailed.RemoveListener(OnReconnectFailed);
        heartRateObserver.onStatusChanged.RemoveListener(SetStatusText);
        heartRateObserver.onBusyChanged.RemoveListener(OnBusyChanged);
    }

    public void OnDeviceSelected() { lastHeartRate = -1; SetStatusText("Discovering heart rate services..."); RefreshIcons(); }
    public void OnServiceFound() { SetStatusText("Discovering heart rate characteristics..."); }
    public void OnCharacteristicFound() { SetStatusText("Heart rate characteristic found. Subscribing..."); }
    public void OnSubscribed() { SetStatusText("Subscribed. Waiting for heart rate..."); RefreshIcons(); }
    public void OnHeartRateReceived(int heartRate)
    {
        RefreshIcons();
        if (heartRate == lastHeartRate) return;
        if (heartRate > 0) SetStatusText("Current heart rate: " + heartRate + " bpm");
        lastHeartRate = heartRate;
    }
    public void OnTimeout() { lastHeartRate = -1; RefreshIcons(); }
    public void OnReconnectFailed() { SetStatusText("Reconnection failed. Select a device to try again."); RefreshIcons(); }
    void OnBusyChanged(bool busy) { RefreshIcons(); }

    void RefreshIcons()
    {
        if (heartRateObserver == null) return;
        var state = heartRateObserver.State;
        bool working = state == HeartRateConnectionState.Scanning || state == HeartRateConnectionState.Connecting || state == HeartRateConnectionState.AwaitingFirstNotification;
        bool connected = state == HeartRateConnectionState.AwaitingFirstNotification || state == HeartRateConnectionState.Connected;
        SetActive(loadingImageObject, working);
        SetActive(connectedImageObject, connected);
    }
    static void SetActive(GameObject target, bool value) { if (target != null && target.activeSelf != value) target.SetActive(value); }
    void SetStatusText(string value)
    {
        if (statusText == null || statusText.text == value) return;
        statusText.text = value;
        // A later valid notification must replace a diagnostic even if its BPM is unchanged.
        lastHeartRate = -1;
    }
}
