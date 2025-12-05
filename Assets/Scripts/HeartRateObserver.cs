using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class HeartRateObserver : MonoBehaviour
{
    private bool isScanningDevices = false;
    private bool isScanningServices = false;
    private bool isScanningCharacteristics = false;
    private bool isSubscribed = false;
    private bool isDataReceived = false;
    [Space]
    public TMP_Text deviceScanButtonText;
    [Space]
    public TMP_Text selectedDeviceNameText;
    public TMP_Text selectedDeviceIDText;
    public TMP_Text errorText;
    [Header("Device, Service, Characteristic IDs")]
    public string selectedDeviceId;
    public string selectedServiceId;
    public string selectedCharacteristicId;
    [Space]
    // public string searchDeviceId = string.Empty;    // if set, will auto select device with matching ID on discovery
    public string searchServiceId = "0000180d"; // Heart Rate Service
    public string searchCharacteristicId = "00002a37"; // Heart Rate Measurement
    [Header("Timeout and Reconnection")]
    public float interupTimeoutSeconds = 5f;
    [Tooltip("Maximum reconnection attempts after timeout. if set to 0, will not attempt to reconnect.")]
    public int maxReconnectAttempts = 0; // maximum reconnection attempts after timeout
    private int reconnectAttempts = 0; // current reconnection attempt count
    public UnityEvent onDeviceTimeout;
    public UnityEvent onDeviceReconnectFailed;
    [Space]
    public BleDeviceButtonManager deviceButtonManager;
    [Space]
    public UnityEvent onDeviceClear;
    public UnityEvent<string, string, bool> onDeviceDiscovered;
    public UnityEvent<string, string> onDeviceNameChanged;
    public UnityEvent<string, bool> onDeviceDeviceConnectableChanged;
    [Space]
    public UnityEvent onDeviceSelected;
    public UnityEvent onServiceFound;
    public UnityEvent onCharacteristicFound;
    public UnityEvent onSubscribed;
    [Space]
    public UnityEvent<int> onHeartRateReceived;
    public UnityEvent<float> onHeartRateReceivedFloat;
    public UnityEvent<float> onHeartRateReceivedHz;

    readonly Dictionary<string, DeviceInfo> devices = new Dictionary<string, DeviceInfo>();
    readonly List<string> services = new List<string>();
    readonly List<string> characteristics = new List<string>();
    string lastError;

    private Coroutine deviceScanRoutine = null;
    private Coroutine serviceScanRoutine = null;
    private Coroutine characteristicScanRoutine = null;
    private Coroutine heartRatePollingRoutine = null;
    private float lastHeartRateReceivedTime = 0f;

    class DeviceInfo
    {
        public string name;
        public bool isConnectable;
        public DeviceInfo(string name, bool isConnectable)
        {
            this.name = name;
            this.isConnectable = isConnectable;
        }
        public DeviceInfo()
        {
            name = "";
            isConnectable = false;
        }
    }

    void Start()
    {
        Reset();
        if (deviceButtonManager != null)
        {
            deviceButtonManager.ClearAll();
            // Add listeners for BLE device events
            onDeviceClear.AddListener(deviceButtonManager.ClearAll);
            onDeviceDiscovered.AddListener(deviceButtonManager.HandleDeviceDiscovered);
            onDeviceNameChanged.AddListener(deviceButtonManager.HandleDeviceNameChanged);
            onDeviceNameChanged.AddListener(UpdateDeviceName);
            onDeviceDeviceConnectableChanged.AddListener(deviceButtonManager.HandleDeviceConnectableChanged);
            // Add listener for selection
            deviceButtonManager.onDeviceSelected.AddListener(SelectDevice);
        }
    }

    void OnDisable()
    {
        Reset();
    }

    void Update()
    {
        // log potential errors
        BleApi.GetError(out BleApi.ErrorMessage errorMessage);
        if (lastError != errorMessage.msg)
        {
            Debug.LogError(errorMessage.msg);
            if (errorText != null)
            {
                errorText.text = errorMessage.msg;
            }
            lastError = errorMessage.msg;
        }

        if (isDataReceived && (lastHeartRateReceivedTime + interupTimeoutSeconds < Time.time))
        {
            Timeout();
            Reset();
            AttemptReconnect();
        }
    }

    private void OnApplicationQuit()
    {
        BleApi.Quit();
    }

    public void Reset()
    {
        if (deviceScanRoutine != null)
        {
            StopCoroutine(deviceScanRoutine);
            deviceScanRoutine = null;
        }
        if (serviceScanRoutine != null)
        {
            StopCoroutine(serviceScanRoutine);
            serviceScanRoutine = null;
        }
        if (characteristicScanRoutine != null)
        {
            StopCoroutine(characteristicScanRoutine);
            characteristicScanRoutine = null;
        }
        if (heartRatePollingRoutine != null)
        {
            StopCoroutine(heartRatePollingRoutine);
            heartRatePollingRoutine = null;
        }
        
        deviceScanButtonText.text = "Start scan";
        
        isScanningDevices = false;
        isScanningServices = false;
        isScanningCharacteristics = false;
        isSubscribed = false;
        isDataReceived = false;
        reconnectAttempts = 0;
        
        BleApi.Quit();
    }

    public void Timeout()
    {
        // Reset heart rate to 0 on timeout
        lastHeartRateReceivedTime = Time.time;
        onHeartRateReceived.Invoke(0);
        onHeartRateReceivedFloat.Invoke(0f);
        onHeartRateReceivedHz.Invoke(0f);
        // trigger timeout event
        onDeviceTimeout.Invoke();
    }

    // Attempt to reconnect after a timeout
    private void AttemptReconnect()
    {
        // Do nothing while no target device set, scanning, or connected.
        if (string.IsNullOrEmpty(selectedDeviceId))
        {
            return;
        }

        if (maxReconnectAttempts > 0)
        {
            if (reconnectAttempts >= maxReconnectAttempts)
            {
                Debug.LogWarning($"Maximum reconnection attempts ({maxReconnectAttempts}) reached. Giving up.");
                onDeviceReconnectFailed.Invoke();
                return;
            }
            reconnectAttempts++;
            Debug.Log($"Reconnection attempt {reconnectAttempts}/{maxReconnectAttempts} for device {selectedDeviceId}.");
        }

        // Disconnect current connection
        Disconnect(selectedDeviceId);
        // Restart device scan to find the device again
        StartDeviceScan();
    }

    public void Disconnect()
    {
        // Disconnect and clear selected device ID to prevent auto-reconnect
        BleApi.Quit();  // Disconnect all devices
        selectedDeviceId = string.Empty;
        Timeout();
    }
    
    public void Disconnect(string deviceId)
    {
        BleApi.Disconnect(deviceId);
        Reset();
    }

    public void StartStopDeviceScan()
    {
        if (!isScanningDevices)
        {
            StartDeviceScan();
        }
        else
        {
            StopDeviceScan();
        }
    }
    public void StartDeviceScan()
    {
        if (isScanningDevices) return;
        // clear previous results
        onDeviceClear.Invoke();
        // start new scan
        BleApi.StartDeviceScan();
        isScanningDevices = true;
        deviceScanButtonText.text = "Stop scan";
        if (deviceScanRoutine == null)
        {
            deviceScanRoutine = StartCoroutine(DeviceScanCoroutine());
        }
    }
    public void StopDeviceScan()
    {
        if (!isScanningDevices) return;
        // stop scan
        isScanningDevices = false;
        BleApi.StopDeviceScan();
        deviceScanButtonText.text = "Start scan";
        if (deviceScanRoutine != null)
        {
            StopCoroutine(deviceScanRoutine);
            deviceScanRoutine = null;
        }
    }
    private IEnumerator DeviceScanCoroutine()
    {
        BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
        while (isScanningDevices)
        {
            BleApi.ScanStatus status;
            do
            {
                status = BleApi.PollDevice(ref res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    if (!devices.TryGetValue(res.id, out DeviceInfo deviceInfo))
                    {
                        // Create empty DeviceInfo when first discovered
                        deviceInfo = new DeviceInfo("", false);
                        devices[res.id] = deviceInfo;
                        onDeviceDiscovered.Invoke(res.id, deviceInfo.name, deviceInfo.isConnectable);
                    }

                    if (res.nameUpdated)
                    {
                        deviceInfo.name = res.name;
                        onDeviceNameChanged.Invoke(res.id, deviceInfo.name);
                    }
                    if (res.isConnectableUpdated)
                    {
                        deviceInfo.isConnectable = res.isConnectable;
                        onDeviceDeviceConnectableChanged.Invoke(res.id, deviceInfo.isConnectable);
                    }
                    
                    // Auto reconnect if the scanned device matches the previously selected device
                    if (!string.IsNullOrEmpty(selectedDeviceId) && res.id == selectedDeviceId && res.isConnectable)
                    {
                        reconnectAttempts = 0;
                        // Stop device scan and start service scan
                        StopDeviceScan();
                        StartServiceScan();
                        yield break;
                    }
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningDevices = false;
                    deviceScanButtonText.text = "Start scan";
                    deviceScanRoutine = null;
                    yield break;
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);

            // spread work across frames
            yield return null;
        }
        // If stopped from outer
        deviceScanRoutine = null;
    }

    // Works with found in device scan
    public void SelectDevice(string selectedId, string selectedName)
    {
        onDeviceSelected.Invoke();
        // Remove all buttons
        deviceButtonManager.ClearAll();
        // Set selected device info
        selectedDeviceNameText.text = selectedName;
        selectedDeviceIDText.text = selectedId;
        // Service scan start
        selectedDeviceId = selectedId;
        StopDeviceScan();
        StartServiceScan();
    }

    // Reconnect to a previously selected device (needs to Re-Scan)
    public void ReconnectDevice(string deviceID, string deviceName)
    {
        if (string.IsNullOrEmpty(deviceID))
        {
            return;
        }
        onDeviceSelected.Invoke();
        // Remove all buttons
        deviceButtonManager.ClearAll();
        // Set selected device info
        selectedDeviceNameText.text = deviceName;
        selectedDeviceIDText.text = deviceID;
        // Start reconnection
        selectedDeviceId = deviceID;
        reconnectAttempts = 0;
        StopDeviceScan();
        StartDeviceScan();
    }

    public void UpdateDeviceName(string id, string name)
    {
        if (!string.IsNullOrEmpty(selectedDeviceId) && id == selectedDeviceId)
        {
            selectedDeviceNameText.text = name;
        }
    }

    public void StartServiceScan()
    {
        if (!isScanningServices)
        {
            services.Clear();
            // start new scan
            BleApi.ScanServices(selectedDeviceId);
            isScanningServices = true;
            if (serviceScanRoutine == null)
            {
                serviceScanRoutine = StartCoroutine(ServiceScanCoroutine());
            }
        }
    }
    IEnumerator ServiceScanCoroutine()
    {
        while (isScanningServices)
        {
            BleApi.ScanStatus status;
            do
            {
                status = BleApi.PollService(out BleApi.Service res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    services.Add(res.uuid);
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningServices = false;
                    serviceScanRoutine = null;
                    OnServiceScanComplete();
                    yield break;
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
            // spread work across frames
            yield return null;
        }
        // If stopped from outer
        serviceScanRoutine = null;
    }
    void OnServiceScanComplete()
    {
        string searchServiceLowerCase = searchServiceId.ToLower();
        foreach (string service in services)
        {
            string serviceId = service.Trim('{', '}').ToLower();
            if (serviceId.StartsWith(searchServiceLowerCase))
            {
                onServiceFound.Invoke();
                selectedServiceId = service;
                StartCharacteristicScan();
                return;
            }
        }

        // not found
        Debug.Log("Not found target service start with: " + searchServiceId);
    }

    public void StartCharacteristicScan()
    {
        if (!isScanningCharacteristics)
        {
            // start new scan
            characteristics.Clear();
            BleApi.ScanCharacteristics(selectedDeviceId, selectedServiceId);
            isScanningCharacteristics = true;
            if (characteristicScanRoutine == null)
            {
                characteristicScanRoutine = StartCoroutine(CharacteristicScanCoroutine());
            }
        }
    }
    IEnumerator CharacteristicScanCoroutine()
    {
        while (isScanningCharacteristics)
        {
            BleApi.ScanStatus status;
            do
            {
                status = BleApi.PollCharacteristic(out BleApi.Characteristic res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    characteristics.Add(res.uuid);
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningCharacteristics = false;
                    characteristicScanRoutine = null;
                    OnCharacteristicScanComplete();
                    yield break;
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);

            // spread work across frames
            yield return null;
        }
        // If stopped from outer
        characteristicScanRoutine = null;
    }
    void OnCharacteristicScanComplete()
    {
        string searchCharacteristicLowerCase = searchCharacteristicId.ToLower();
        foreach (string characteristic in characteristics)
        {
            string characteristicId = characteristic.Trim('{','}').ToLower();
            if (characteristicId.StartsWith(searchCharacteristicLowerCase))
            {
                onCharacteristicFound.Invoke();
                selectedCharacteristicId = characteristic;
                lastHeartRateReceivedTime = Time.time;
                isDataReceived = false;
                Subscribe();
                return;
            }
        }
        // not found
        Debug.Log("Not found target characteristic start with: " + searchCharacteristicId);
    }

    public void Subscribe()
    {
        if (heartRatePollingRoutine != null)
        {
            StopCoroutine(heartRatePollingRoutine);
            heartRatePollingRoutine = null;
        }
        // Save device info when subscribing(successful connection)
        AppSettingsManager.SaveDeviceInfo(selectedDeviceId, selectedDeviceNameText.text);
        // Start polling subscription
        BleApi.SubscribeCharacteristic(selectedDeviceId, selectedServiceId, selectedCharacteristicId, false);
        isSubscribed = true;
        onSubscribed.Invoke();
        heartRatePollingRoutine = StartCoroutine(PollSubscriptionCoroutine());
    }
    public void Unsubscribe()
    {
        isSubscribed = false;
        if (heartRatePollingRoutine != null)
        {
            StopCoroutine(heartRatePollingRoutine);
            heartRatePollingRoutine = null;
        }
        Debug.Log("Unsubscribe: " + selectedCharacteristicId);
    }
    IEnumerator PollSubscriptionCoroutine()
    {
        while (isSubscribed)
        {
            while (BleApi.PollData(out BleApi.BLEData res, false))
            {
                if (HeartRateParser.TryParseHeartRate(res.buf, res.size, out HeartRateParser.HeartRateMeasurement measureData, out string error))
                {
                    isDataReceived = true;
                    lastHeartRateReceivedTime = Time.time;
                    // Reset reconnection attempts on successful data
                    reconnectAttempts = 0;

                    onHeartRateReceived.Invoke(measureData.HeartRate);
                    onHeartRateReceivedFloat.Invoke((float)measureData.HeartRate);

                    float heartRateHz = measureData.HeartRate / 60f;
                    onHeartRateReceivedHz.Invoke(heartRateHz);
                }
                else
                {
                    Debug.LogError("Failed to parse heart rate data: " + error);
                    continue;
                }
            }
            // spread work across frames
            yield return null;
        }
    }
}
