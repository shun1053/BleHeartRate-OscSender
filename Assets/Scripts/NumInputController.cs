using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

public class NumInputController : MonoBehaviour
{
    public TMP_InputField inputField;
    [Space]
    public bool limitMin = true;
    public float limitMinValue = 0;
    [Space]
    public bool limitMax = true;
    public float limitMaxValue = 0;
    [Space]
    public ButtonSetting[] buttonSettings;
    [Space]
    public UnityEvent<float> onValueChanged;

    [Serializable]
    public struct ButtonSetting
    {
        public Button button;
        public int addValue;
    }

    private float currentValue = 0f;

    // Start is called before the first frame update
    void Start()
    {
        if (inputField == null)
        {
            return;
        }

        if (!float.TryParse(inputField.text, out currentValue))
        {
            if (limitMin && limitMax)
            {
                currentValue = Mathf.Clamp(currentValue, limitMinValue, limitMaxValue);
            }
            else if (limitMin)
            {
                currentValue = Mathf.Max(currentValue, limitMinValue);
            }
            else if (limitMax)
            {
                currentValue = Mathf.Min(currentValue, limitMaxValue);
            }
            else
            {
                currentValue = 0f;
            }
            inputField.text = currentValue.ToString();
        }
        inputField.onEndEdit.Invoke(inputField.text);
        onValueChanged.Invoke(currentValue);

        foreach (ButtonSetting setting in buttonSettings)
        {
            setting.button.onClick.AddListener(() => {
                AddValue(setting.addValue);
            });
        }
    }

    public void AddValue(float addValue)
    {
        if (inputField == null)
        {
            return;
        }

        if (float.TryParse(inputField.text, out currentValue))
        {
            currentValue += addValue;
            if (limitMin && limitMax)
            {
                currentValue = Mathf.Clamp(currentValue, limitMinValue, limitMaxValue);
            }
            else if (limitMin)
            {
                currentValue = Mathf.Max(currentValue, limitMinValue);
            }
            else if (limitMax)
            {
                currentValue = Mathf.Min(currentValue, limitMaxValue);
            }
            inputField.text = currentValue.ToString();
            inputField.onEndEdit.Invoke(inputField.text);
            onValueChanged.Invoke(currentValue);
        }
    }

    public void SetMaxValue(float maxValue)
    {
        limitMaxValue = maxValue;
    }
    public void SetMinValue(float minValue)
    {
        limitMinValue = minValue;
    }
}
