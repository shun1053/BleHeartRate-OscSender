using UnityEngine;
using TMPro;

public class ConnectionStatusController : MonoBehaviour
{
    public HeartRateObserver heartRateObserver;
    public GameObject loadingImageObject;
    public GameObject connectedImageObject;
    public TMP_Text statusText;
    
    void Start()
    {
        if (heartRateObserver != null)
        {
            heartRateObserver.onDeviceSelected.AddListener(OnDeviceSelected);
            heartRateObserver.onServiceFound.AddListener(OnServiceFound);
            heartRateObserver.onCharacteristicFound.AddListener(OnCharacteristicFound);
            heartRateObserver.onSubscribed.AddListener(OnSubscribed);
            heartRateObserver.onHeartRateReceived.AddListener(OnHeartRateReceived);
            heartRateObserver.onDeviceTimeout.AddListener(OnTimeout);
            heartRateObserver.onDeviceReconnectFailed.AddListener(OnReconnectFailed);
            SetStatusText("");
        }
    }

    public void OnDeviceSelected()
    {
        SetIconObjects(false);
        SetStatusText("Discovering heart rate services...");
    }

    public void OnServiceFound()
    {
        SetIconObjects(false);
        SetStatusText("Discovering heart rate characteristics...");
    }

    public void OnCharacteristicFound()
    {
        SetIconObjects(false);
        SetStatusText("Heartrate characteristic found. Subscribing...");
    }

    public void OnSubscribed()
    {
        SetIconObjects(true);
        SetStatusText("Receiving will start soon!");
    }

    public void OnHeartRateReceived(int heartRate)
    {
        SetIconObjects(true);
        SetStatusText("Current heart rate: " + heartRate.ToString() + " bpm");
    }

    public void OnTimeout()
    {
        SetIconObjects(false);
        SetStatusText("Connection timed out. Reconnecting...");
    }

    public void OnReconnectFailed()
    {
        SetIconObjects(false);
        SetStatusText("Reconnection failed. Retrying...");
    }

    void SetIconObjects(bool isConnected)
    {
        if (loadingImageObject != null)
        {
            loadingImageObject.SetActive(!isConnected);
        }
        if (connectedImageObject != null)
        {
            connectedImageObject.SetActive(isConnected);
        }
    }

    void SetStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }
}