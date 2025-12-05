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
        if (ipAddressInputField != null)
        {
            ipAddressInputField.text = TargetIPAddress;
            ipAddressInputField.onEndEdit.AddListener(ApplyIPAddress);
        }
        if (portInputField != null)
        {
            portInputField.text = TargetPort.ToString();
            portInputField.onEndEdit.AddListener(ApplyPort);
        }
        if (heartRateSendTargetInputField != null && heartRateOscValueSender != null)
        {
            heartRateSendTargetInputField.text = heartRateOscValueSender.valueSendTarget;
            heartRateSendTargetInputField.onEndEdit.AddListener(ApplyHeartRateSendTarget);
        }
        if (normalizedHeartRateSendTargetInputField != null && normalizedHeartRateOscValueSender != null)
        {
            normalizedHeartRateSendTargetInputField.text = normalizedHeartRateOscValueSender.valueSendTarget;
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
        else
        {
            if (ipAddressInputField != null)
            {
                ipAddressInputField.text = oscClient.address;
            }
        }
    }

    public void ApplyPort(string portText)
    {
        if (int.TryParse(portText, out int port) && (port >= 0 && port <= 65535))
        {
            oscClient.port = port;
            return;
        }
        if (portInputField != null)
        {
            portInputField.text = oscClient.port.ToString();
        }
    }

    public void ApplyHeartRateSendTarget(string sendTarget)
    {
        if (heartRateOscValueSender != null)
        {
            heartRateOscValueSender.valueSendTarget = sendTarget;
        }
    }

    public void ApplyNormalizedHeartRateSendTarget(string sendTarget)
    {
        if (normalizedHeartRateOscValueSender != null)
        {
            normalizedHeartRateOscValueSender.valueSendTarget = sendTarget;
        }
    }
}