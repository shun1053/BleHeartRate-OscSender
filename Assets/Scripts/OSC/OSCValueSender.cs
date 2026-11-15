using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using uOSC;

public class OSCValueSender : MonoBehaviour
{
    [SerializeField]private uOscClient oscClient;
    [Space]
    public string valueSendTarget = "/avatar/parameters/name";
    
    void Start()
    {
        if (oscClient == null)
        {
            enabled = false;
        }
    }

    public void SendValue(float sendValue)
    {
        if (oscClient != null)
        {
            oscClient.Send(valueSendTarget, sendValue);
        }
    }
    public void SendValueDivided(float sendValue, float divisor)
    {
        if (oscClient == null)
        {
            Debug.LogWarning("OSC client is not assigned.");
            return;
        }

        if (Mathf.Approximately(divisor, 0f))
        {
            Debug.LogWarning("Divisor is zero. Send aborted to avoid division by zero.");
            return;
        }

        float valueToSend = sendValue / divisor;
        oscClient.Send(valueSendTarget, valueToSend);
    }
}
