using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using uOSC;

[RequireComponent(typeof(uOscClient))]
public class OSCValueSender : MonoBehaviour
{
    private uOscClient oscClient;
    [Space]
    public string valueSendTarget = "/avatar/parameters/name";
    
    void Start()
    {
        oscClient = GetComponent<uOscClient>();
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
}
