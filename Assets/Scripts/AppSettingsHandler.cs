using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AppSettingsHandler : MonoBehaviour
{
    public GameObject[] deviceSelectUIObjects;
    public GameObject[] connectionUIObjects;
    [Header("Loaded settings event(ID, Name)")]
    public UnityEvent<string, string> onLastDeviceLoaded;
    [Header("Loaded settings event(Min)")]
    public UnityEvent<float> onLastSettingMinLoaded;
    [Header("Loaded settings event(Max)")]
    public UnityEvent<float> onLastSettingMaxLoaded;

    IEnumerator Start()
    {
        // Wait a frame to ensure other initializations are done
        yield return null;
        if (AppSettingsManager.Load(out AppSettings settings))
        {
            if (!string.IsNullOrEmpty(settings.DeviceID))
            {
                onLastDeviceLoaded.Invoke(settings.DeviceID, settings.DeviceName);
                SwitchUI(true);
            }
        }
        else
        {
            SwitchUI(false);
        }
        onLastSettingMinLoaded.Invoke(settings.MinValue);
        onLastSettingMaxLoaded.Invoke(settings.MaxValue);
    }

    void SwitchUI(bool isDeviceSelected)
    {
        SetActiveAll(deviceSelectUIObjects, !isDeviceSelected);
        SetActiveAll(connectionUIObjects, isDeviceSelected);
    }

    void SetActiveAll(GameObject[] objects, bool isActive)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }
    }
}