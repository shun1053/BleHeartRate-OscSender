using UnityEngine;
using UnityEngine.Events;

public class NumberToStringEvent : MonoBehaviour
{
    public UnityEvent<string> stringEvent;
    public string numberFormat = "0.##";

    public void OnFloatReceived(float value)
    {
        stringEvent.Invoke(value.ToString(numberFormat));
    }

    public void OnIntReceived(int value)
    {
        stringEvent.Invoke(value.ToString(numberFormat));
    }
}
