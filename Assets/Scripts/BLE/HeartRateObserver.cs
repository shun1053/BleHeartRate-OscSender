using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class HeartRateObserver : MonoBehaviour
{
    public TMP_Text deviceScanButtonText;
    public TMP_Text selectedDeviceNameText, selectedDeviceIDText, errorText;
    [Header("Device, Service, Characteristic IDs")]
    public string selectedDeviceId, selectedServiceId, selectedCharacteristicId;
    public string searchServiceId = "0000180d", searchCharacteristicId = "00002a37";
    [Header("Timeout and Reconnection")]
    public float interupTimeoutSeconds = 5f;
    public float scanTimeoutSeconds = 30f, connectionTimeoutSeconds = 30f, firstNotificationTimeoutSeconds = 10f;
    [Tooltip("Maximum automatic retry attempts. 0 means unlimited; successful heart rate data resets the count.")]
    public int maxReconnectAttempts;
    public BleDeviceButtonManager deviceButtonManager;

    public UnityEvent onDeviceTimeout = new UnityEvent();
    public UnityEvent onDeviceReconnectFailed = new UnityEvent();
    public UnityEvent onDeviceClear = new UnityEvent();
    public UnityEvent<string, string, bool> onDeviceDiscovered = new UnityEvent<string, string, bool>();
    public UnityEvent<string, string> onDeviceNameChanged = new UnityEvent<string, string>();
    public UnityEvent<string, bool> onDeviceDeviceConnectableChanged = new UnityEvent<string, bool>();
    public UnityEvent onDeviceSelected = new UnityEvent();
    public UnityEvent onServiceFound = new UnityEvent();
    public UnityEvent onCharacteristicFound = new UnityEvent();
    public UnityEvent onSubscribed = new UnityEvent();
    public UnityEvent<int> onHeartRateReceived = new UnityEvent<int>();
    public UnityEvent<float> onHeartRateReceivedFloat = new UnityEvent<float>();
    public UnityEvent<float> onHeartRateReceivedHz = new UnityEvent<float>();
    public UnityEvent<string> onConnectionFailed = new UnityEvent<string>();
    public UnityEvent<string> onStatusChanged = new UnityEvent<string>();
    public UnityEvent<bool> onBusyChanged = new UnityEvent<bool>();
    public UnityEvent onReturnToDeviceList = new UnityEvent();

    HeartRateSession session;
    IBleBackend activeBackend;
    IBleBackend configuredBackend;
    IBleClock configuredClock;
    bool listenersAttached, nativeUnavailable;
    readonly Dictionary<string, string> displayedNames = new Dictionary<string, string>();
    readonly Dictionary<string, bool> displayedConnectable = new Dictionary<string, bool>();

    public HeartRateConnectionState State { get { return session == null || nativeUnavailable ? HeartRateConnectionState.Idle : session.State; } }
    public bool IsScanning { get { return State == HeartRateConnectionState.Scanning; } }
    public bool IsBusy { get { return !nativeUnavailable && session != null && session.IsBusy; } }
    public bool ShowAllDevices { get { return session != null && session.ShowAll; } }

    sealed class UnityClock : IBleClock { public double Now { get { return Time.realtimeSinceStartupAsDouble; } } }

    // May be configured before Start for deterministic validation without accessing a Bluetooth adapter.
    public void ConfigureBackend(IBleBackend backend, IBleClock clock)
    {
        if (session != null) throw new InvalidOperationException("Configure the BLE backend before starting the observer.");
        configuredBackend = backend ?? throw new ArgumentNullException(nameof(backend));
        configuredClock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    void Start() { Run(EnsureSession); }

    void EnsureSession()
    {
        if (nativeUnavailable) throw new InvalidOperationException("BLE is unavailable. Restart after checking the Bluetooth adapter and native plugin.");
        if (session != null) return;
        AppSettingsManager.Load(out AppSettings settings);
        activeBackend = configuredBackend ?? new NativeHeartRateBackend();
        session = new HeartRateSession(activeBackend, configuredClock ?? new UnityClock(), settings.VerifiedHeartRateDeviceIds);
        ApplyConfiguration();
        session.DevicesCleared += () => { displayedNames.Clear(); displayedConnectable.Clear(); onDeviceClear.Invoke(); };
        session.DeviceChanged += PublishDevice;
        session.StateChanged += HandleStateChanged;
        session.StageSucceeded += HandleStage;
        session.HeartRateReceived += PublishHeartRate;
        session.DeviceVerified += (id, name) =>
        {
            try { AppSettingsManager.SaveVerifiedDeviceInfo(id, name); }
            catch (Exception exception) { ReportError("Connected, but device settings could not be saved: " + exception.Message); }
        };
        session.ReceptionReset += () => PublishHeartRate(0);
        session.TimedOut += onDeviceTimeout.Invoke;
        session.ReconnectFailed += onDeviceReconnectFailed.Invoke;
        session.ReturnToDeviceList += onReturnToDeviceList.Invoke;
        session.Error += ReportError;
        if (deviceButtonManager != null && !listenersAttached)
        {
            deviceButtonManager.ClearAll();
            onDeviceClear.AddListener(deviceButtonManager.ClearAll);
            onDeviceDiscovered.AddListener(deviceButtonManager.HandleDeviceDiscovered);
            onDeviceNameChanged.AddListener(deviceButtonManager.HandleDeviceNameChanged);
            onDeviceDeviceConnectableChanged.AddListener(deviceButtonManager.HandleDeviceConnectableChanged);
            deviceButtonManager.onDeviceSelected.AddListener(SelectDevice);
            listenersAttached = true;
        }
        HandleStateChanged(HeartRateConnectionState.Idle);
    }

    void Update()
    {
        if (session == null || nativeUnavailable) return;
        try
        {
            ApplyConfiguration();
            session.Tick();
        }
        catch (Exception exception) { HandleException(exception); }
    }

    void ApplyConfiguration()
    {
        session.ScanTimeout = scanTimeoutSeconds;
        session.ConnectionTimeout = connectionTimeoutSeconds;
        session.FirstNotificationTimeout = firstNotificationTimeoutSeconds;
        session.NotificationTimeout = interupTimeoutSeconds;
        session.MaxReconnectAttempts = maxReconnectAttempts;
    }

    void OnDisable() { if (session != null) Run(() => session.Cancel(false)); }
    void OnApplicationQuit() { if (session != null) Run(() => session.Cancel(false)); }

    void Run(Action action)
    {
        if (nativeUnavailable) return;
        try { action(); }
        catch (Exception exception)
        { HandleException(exception); }
    }

    void HandleException(Exception exception)
    {
        if (nativeUnavailable) return;
        bool hadReception = State == HeartRateConnectionState.Connected;
        // Latch first: returning to the list can disable this component and re-enter OnDisable.
        nativeUnavailable = true;
        if (activeBackend != null)
        {
            BestEffort(activeBackend.StopDeviceScan);
            BestEffort(activeBackend.CancelConnection);
        }
        if (hadReception) BestEffort(() => PublishHeartRate(0));
        if (deviceButtonManager != null) BestEffort(() => deviceButtonManager.SetSelectionEnabled(false));
        BestEffort(() => onBusyChanged.Invoke(false));
        BestEffort(() => onReturnToDeviceList.Invoke());
        BestEffort(() => ReportError("BLE operation failed: " + exception.Message));
        Debug.LogException(exception, this);
    }

    static void BestEffort(Action action)
    {
        try { action(); }
        catch (Exception) { /* Preserve the original failure while still attempting the other cleanup. */ }
    }

    public void Reset() { if (Application.isPlaying && session != null) Run(() => session.Cancel(false)); }
    public void StartStopDeviceScan() { if (IsScanning) StopDeviceScan(); else StartDeviceScan(); }
    public void StartDeviceScan()
    {
        Run(() => { EnsureSession(); ClearError(); session.StartScan(); selectedDeviceId = string.Empty; });
    }
    public void StopDeviceScan() { if (session != null) Run(session.StopScan); }
    public void SetShowAllDevices(bool value) { Run(() => { EnsureSession(); session.SetShowAll(value); }); }

    public void SelectDevice(string id, string name) { Select(id, name, false); }
    public void ReconnectDevice(string id, string name) { Select(id, name, true); }
    void Select(string id, string name, bool savedDevice)
    {
        if (string.IsNullOrEmpty(id) || IsBusy || State == HeartRateConnectionState.Connected) return;
        Run(() =>
        {
            EnsureSession();
            ClearError();
            selectedDeviceId = id;
            selectedServiceId = selectedCharacteristicId = string.Empty;
            SetText(selectedDeviceNameText, string.IsNullOrEmpty(name) ? "Unnamed device" : name);
            SetText(selectedDeviceIDText, id);
            onDeviceSelected.Invoke();
            session.Select(id, name, savedDevice);
        });
    }

    public void CancelConnection()
    {
        if (session != null) Run(() => session.Cancel());
        selectedDeviceId = selectedServiceId = selectedCharacteristicId = string.Empty;
        SetText(deviceScanButtonText, "Start scan");
    }
    public void Disconnect() { CancelConnection(); PublishHeartRate(0); }
    public void Disconnect(string deviceId) { if (selectedDeviceId == deviceId) Disconnect(); }
    public void Unsubscribe() { CancelConnection(); }

    // Retained for existing serialized UnityEvents. The native operation owns all three stages.
    public void StartServiceScan() { if (!IsBusy) SelectDevice(selectedDeviceId, selectedDeviceNameText == null ? null : selectedDeviceNameText.text); }
    public void StartCharacteristicScan() { StartServiceScan(); }
    public void Subscribe() { StartServiceScan(); }

    public void Timeout() { PublishHeartRate(0); onDeviceTimeout.Invoke(); }
    public void UpdateDeviceName(string id, string name) { if (id == selectedDeviceId) SetText(selectedDeviceNameText, name); }

    void PublishDevice(HeartRateSession.Device device)
    {
        bool published = displayedNames.TryGetValue(device.Id, out string oldName);
        if (!published && device.Visible)
        {
            displayedNames.Add(device.Id, device.Name);
            displayedConnectable.Add(device.Id, device.IsConnectable);
            onDeviceDiscovered.Invoke(device.Id, device.Name, device.IsConnectable);
        }
        else if (published)
        {
            if (oldName != device.Name)
            {
                displayedNames[device.Id] = device.Name;
                onDeviceNameChanged.Invoke(device.Id, device.Name);
                UpdateDeviceName(device.Id, device.Name);
            }
            if (displayedConnectable[device.Id] != device.IsConnectable)
            {
                displayedConnectable[device.Id] = device.IsConnectable;
                onDeviceDeviceConnectableChanged.Invoke(device.Id, device.IsConnectable);
            }
        }
        if (deviceButtonManager != null) deviceButtonManager.SetDeviceVisible(device.Id, device.Visible);
    }

    void HandleStateChanged(HeartRateConnectionState state)
    {
        SetText(deviceScanButtonText, IsScanning ? "Stop scan" : "Start scan");
        if (deviceButtonManager != null) deviceButtonManager.SetSelectionEnabled(!IsBusy && state != HeartRateConnectionState.Connected);
        onBusyChanged.Invoke(IsBusy || IsScanning);
        switch (state)
        {
            case HeartRateConnectionState.Scanning: onStatusChanged.Invoke("Scanning for heart rate devices..."); break;
            case HeartRateConnectionState.Connecting: onStatusChanged.Invoke("Discovering heart rate services..."); break;
            case HeartRateConnectionState.AwaitingFirstNotification: onStatusChanged.Invoke("Subscribed. Waiting for heart rate..."); break;
            case HeartRateConnectionState.ReconnectWaiting: onStatusChanged.Invoke("Waiting to reconnect..."); break;
            case HeartRateConnectionState.Idle: onStatusChanged.Invoke("Select a heart rate device."); break;
        }
    }

    void HandleStage(int stage)
    {
        if (stage == 1) { selectedServiceId = "0000180d-0000-1000-8000-00805f9b34fb"; onServiceFound.Invoke(); }
        else if (stage == 2) { selectedCharacteristicId = "00002a37-0000-1000-8000-00805f9b34fb"; onCharacteristicFound.Invoke(); }
        else if (stage == 3) onSubscribed.Invoke();
    }

    void PublishHeartRate(int bpm)
    {
        onHeartRateReceived.Invoke(bpm);
        onHeartRateReceivedFloat.Invoke(bpm);
        onHeartRateReceivedHz.Invoke(bpm / 60f);
    }
    void ClearError() { SetText(errorText, string.Empty); }
    void ReportError(string message) { SetText(errorText, message); onConnectionFailed.Invoke(message); onStatusChanged.Invoke(message); }
    static void SetText(TMP_Text target, string value) { if (target != null && target.text != value) target.text = value; }
}
