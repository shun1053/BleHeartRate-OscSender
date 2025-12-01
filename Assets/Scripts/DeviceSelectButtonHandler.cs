using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DeviceSelectButtonHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text deviceNameText;
    [SerializeField] private TMP_Text deviceIdText;
    [SerializeField] private Button button;

    public UnityEvent<string, string> onDeviceSelected = new UnityEvent<string, string>();

    public string DeviceId { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    public void AddSelectListener(UnityAction<string, string> listener)
    {
        onDeviceSelected.AddListener(listener);
    }

    public void SetDeviceInfo(string deviceId, string deviceName, bool showState)
    {
        SetDeviceInfo(deviceId, deviceName);
        SetShowState(showState);
    }
    
    public void SetDeviceInfo(string deviceId, string deviceName)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;

        if (deviceNameText != null)
        {
            deviceNameText.text = deviceName ?? string.Empty;
        }

        if (deviceIdText != null)
        {
            deviceIdText.text = deviceId ?? string.Empty;
        }
    }
    public void SetShowState(bool showState)
    {
        gameObject.SetActive(showState);
    }

    public void OnButtonClicked()
    {
        onDeviceSelected.Invoke(DeviceId, DeviceName);
    }
}
