using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BleDeviceButtonManager : MonoBehaviour
{
    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject deviceButtonPrefab;
    [SerializeField] private Transform contentParent;
    [Space]
    public UnityEvent<string, string> onDeviceSelected = new UnityEvent<string, string>();

    private class DeviceInfo
    {
        public string name;
        public bool isConnectable;
        public DeviceInfo(string name, bool isConnectable)
        {
            this.name = name;
            this.isConnectable = isConnectable;
        }
    }

    private readonly Dictionary<string, DeviceInfo> _devices = new Dictionary<string, DeviceInfo>();
    private readonly Dictionary<string, DeviceSelectButtonHandler> _buttonHandlers = new Dictionary<string, DeviceSelectButtonHandler>();

    // Set all info when a device is discovered
    public void HandleDeviceDiscovered(string deviceId, string deviceName, bool isConnectable)
    {
        if (string.IsNullOrEmpty(deviceId)) return;
        if (!_devices.TryGetValue(deviceId, out DeviceInfo deviceInfo))
        {
            _devices[deviceId] = new DeviceInfo(deviceName, isConnectable);
        }
        else
        {
            deviceInfo.name = deviceName;
            deviceInfo.isConnectable = isConnectable;
        }
        UpdateButton(deviceId);
    }

    // Update only the name when it changes
    public void HandleDeviceNameChanged(string deviceId, string deviceName)
    {
        if (string.IsNullOrEmpty(deviceId)) return;
        if (!_devices.TryGetValue(deviceId, out DeviceInfo deviceInfo))
        {
            _devices[deviceId] = new DeviceInfo(deviceName, false);
        }
        else
        {
            deviceInfo.name = deviceName;
        }
        UpdateButton(deviceId);
    }

    // Update only the connectable state when it changes
    public void HandleDeviceConnectableChanged(string deviceId, bool isConnectable)
    {
        if (string.IsNullOrEmpty(deviceId)) return;
        if (!_devices.TryGetValue(deviceId, out DeviceInfo deviceInfo))
        {
            _devices[deviceId] = new DeviceInfo("", false);
        }
        else
        {
            deviceInfo.isConnectable = isConnectable;
        }
        UpdateButton(deviceId);
    }

    // Clear all devices and buttons(e.g., when restarting a scan)
    public void ClearAll()
    {
        foreach (DeviceSelectButtonHandler buttonHandler in _buttonHandlers.Values)
        {
            if (buttonHandler != null)
            {
                Destroy(buttonHandler.gameObject);
            }
        }
        _devices.Clear();
        _buttonHandlers.Clear();
    }

    private void UpdateButton(string deviceId)
    {
        if (!_devices.TryGetValue(deviceId, out DeviceInfo info)) return;

        bool showState = !string.IsNullOrEmpty(info.name) && info.isConnectable;
        if (_buttonHandlers.TryGetValue(deviceId, out DeviceSelectButtonHandler buttonHandler))
        {
            buttonHandler.SetDeviceInfo(deviceId, info.name);
            buttonHandler.SetShowState(showState);
        }
        else if (buttonHandler == null && showState)
        {
            // Create new button if it doesn't exist and should be shown
            if (deviceButtonPrefab == null)
            {
                Debug.LogWarning("Prefab is not set!");
                return;
            }
            GameObject buttonObj = Instantiate(deviceButtonPrefab, contentParent);
            buttonHandler = buttonObj.GetComponentInChildren<DeviceSelectButtonHandler>();
            if (buttonHandler == null)
            {
                Debug.LogWarning("Prefab does not have DeviceSelectButtonHandler!");
                Destroy(buttonObj);
                return;
            }
            // Add listener for selection(only once on creation)
            buttonHandler.AddSelectListener(onDeviceSelected.Invoke);
            _buttonHandlers[deviceId] = buttonHandler;

            buttonHandler.SetDeviceInfo(deviceId, info.name);
            buttonHandler.SetShowState(showState);
        }
        else
        {
            return;
        }
    }
}

