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
    public float defaultMinValue = 40f;
    public UnityEvent<float> onLastSettingMinLoaded;
    [Header("Loaded settings event(Max)")]
    public float defaultMaxValue = 140f;
    public UnityEvent<float> onLastSettingMaxLoaded;
    [Space]
    [Header("Loaded settings event(QuarterHRSendTarget)")]
    public string defaultQuarterHRSendTarget = "/avatar/parameters/HeartRateQuarterHz";
    public UnityEvent<string> onLastQuarterHRSendTargetLoaded;
    [Header("Loaded settings event(NormalizedValueSendTarget)")]
    public string defaultNormalizedValueSendTarget = "/avatar/parameters/NormalizedHeartRate";
    public UnityEvent<string> onLastNormalizedValueSendTargetLoaded;
    [Header("Loaded settings event(SendTargetIP)")]
    public string defaultSendTargetIP = "127.0.0.1";
    public UnityEvent<string> onLastSendTargetIPLoaded;
    [Header("Loaded settings event(SendTargetPort)")]
    public int defaultSendTargetPort = 9000;
    public UnityEvent<int> onLastSendTargetPortLoaded;

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

            if (!string.IsNullOrEmpty(settings.QuarterHRSendTarget))
            {
                onLastQuarterHRSendTargetLoaded.Invoke(settings.QuarterHRSendTarget);
            }
            else
            {
                onLastQuarterHRSendTargetLoaded.Invoke(defaultQuarterHRSendTarget);
            }

            if (!string.IsNullOrEmpty(settings.NormalizedValueSendTarget))
            {
                onLastNormalizedValueSendTargetLoaded.Invoke(settings.NormalizedValueSendTarget);
            }
            else
            {
                onLastNormalizedValueSendTargetLoaded.Invoke(defaultNormalizedValueSendTarget);
            }

            if (!string.IsNullOrEmpty(settings.SendTargetIP))
            {
                onLastSendTargetIPLoaded.Invoke(settings.SendTargetIP);
            }
            else
            {
                onLastSendTargetIPLoaded.Invoke(defaultSendTargetIP);
            }

            if (settings.SendTargetPort != 0)
            {
                onLastSendTargetPortLoaded.Invoke(settings.SendTargetPort);
            }
            else
            {
                onLastSendTargetPortLoaded.Invoke(defaultSendTargetPort);
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