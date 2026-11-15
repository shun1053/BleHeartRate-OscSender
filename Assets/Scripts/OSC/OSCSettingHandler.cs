using System.Net;
using UnityEngine;
using TMPro;
using uOSC;
using System;

public class OSCSettingHandler : MonoBehaviour
{
    public uOscClient oscClient;
    public TMP_InputField ipAddressInputField;
    public TMP_InputField portInputField;
    [Space]
    public float heartRateDivisor = 4f;
    public HeartRateObserver heartRateObserver;
    public TMP_InputField heartRateSendTargetInputField;
    public OSCValueSender heartRateOscValueSender;
    [Space]
    public HeartRateGraphController heartRateGraphController;
    public TMP_InputField normalizedHeartRateSendTargetInputField;
    public OSCValueSender normalizedHeartRateOscValueSender;
    [Space]
    public AppSettingsHandler appSettingsHandler;

    public string TargetIPAddress
    {
        get { return oscClient.address; }
        set { oscClient.address = value; }
    }

    public int TargetPort
    {
        get { return oscClient.port; }
        set { oscClient.port = value; }
    }

    void Start()
    {
        if (appSettingsHandler != null)
        {
            appSettingsHandler.onLastQuarterHRSendTargetLoaded.AddListener(ApplyHeartRateSendTarget);
            appSettingsHandler.onLastNormalizedValueSendTargetLoaded.AddListener(ApplyNormalizedHeartRateSendTarget);
            appSettingsHandler.onLastSendTargetIPLoaded.AddListener(ApplyIPAddress);
            appSettingsHandler.onLastSendTargetPortLoaded.AddListener(ApplyPort);
        }
        else
        {
            if (heartRateOscValueSender != null)
            {
                ApplyHeartRateSendTarget(heartRateOscValueSender.valueSendTarget);
            }
            if (normalizedHeartRateOscValueSender != null)
            {
                ApplyNormalizedHeartRateSendTarget(normalizedHeartRateOscValueSender.valueSendTarget);
            }
            ApplyIPAddress(TargetIPAddress);
            ApplyPort(TargetPort);
        }

        if (ipAddressInputField != null)
        {
            ipAddressInputField.onEndEdit.AddListener(ApplyIPAddress);
        }
        if (portInputField != null)
        {
            portInputField.onEndEdit.AddListener(ApplyPort);
        }
        if (heartRateSendTargetInputField != null)
        {
            heartRateSendTargetInputField.onEndEdit.AddListener(ApplyHeartRateSendTarget);
        }
        if (normalizedHeartRateSendTargetInputField != null)
        {
            normalizedHeartRateSendTargetInputField.onEndEdit.AddListener(ApplyNormalizedHeartRateSendTarget);
        }
        if (heartRateObserver != null && heartRateOscValueSender != null)
        {
            heartRateObserver.onHeartRateReceivedHz.AddListener((float hr_hz) =>
            {
                heartRateOscValueSender.SendValueDivided(hr_hz, heartRateDivisor);
            });
        }
        if (heartRateGraphController != null && normalizedHeartRateOscValueSender != null)
        {
            heartRateGraphController.onNormalizedValueChanged.AddListener((float normalized_hr) =>
            {
                normalizedHeartRateOscValueSender.SendValue(normalized_hr);
            });
        }
    }

    public void ApplyIPAddress(string ipAddress)
    {
        if (IPAddress.TryParse(ipAddress, out IPAddress parsedIp) || Uri.IsWellFormedUriString(ipAddress, UriKind.Absolute))
        {
            oscClient.address = parsedIp.ToString();
        }
        if (ipAddressInputField != null)
        {
            ipAddressInputField.text = oscClient.address;
        }
        AppSettingsManager.SaveSendTargetIP(oscClient.address);
    }

    public void ApplyPort(string portText)
    {
        if (int.TryParse(portText, out int port))
        {
            ApplyPort(port);
        }
        else
        {
            portInputField.text = oscClient.port.ToString();
        }
        AppSettingsManager.SaveSendTargetPort(oscClient.port);
    }
    public void ApplyPort(int portInt)
    {
        if (portInt >= 0 && portInt <= 65535)
        {
            oscClient.port = portInt;
            return;
        }
        if (portInputField != null)
        {
            portInputField.text = oscClient.port.ToString();
        }
        AppSettingsManager.SaveSendTargetPort(oscClient.port);
    }

    public void ApplyHeartRateSendTarget(string sendTarget)
    {
        if (heartRateOscValueSender != null)
        {
            heartRateOscValueSender.valueSendTarget = sendTarget;
        }
        if (heartRateSendTargetInputField != null)
        {
            heartRateSendTargetInputField.text = sendTarget;
        }
        AppSettingsManager.SaveQuarterHRSendTarget(sendTarget);
    }

    public void ApplyNormalizedHeartRateSendTarget(string sendTarget)
    {
        if (normalizedHeartRateOscValueSender != null)
        {
            normalizedHeartRateOscValueSender.valueSendTarget = sendTarget;
        }
        if (normalizedHeartRateSendTargetInputField != null)
        {
            normalizedHeartRateSendTargetInputField.text = sendTarget;
        }
        AppSettingsManager.SaveNormalizedValueSendTarget(sendTarget);
    }
}