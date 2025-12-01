using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HeartRateGraphController : MonoBehaviour
{
    public Animator heartrateAnimator;
    public string heartrateAnimatorParam = "HeartrateHz";
    [Space]
    public GameObject[] graphOnObjects;
    public GameObject[] graphOffObjects;
    [Header("Normalization Settings")]
    public float highLineStartOffset = 0.9f;
    [SerializeField]private float currentHeartRate;
    [SerializeField]private float minValue;
    [SerializeField]private float maxValue;
    [Space]
    public float normalizedValue;
    public UnityEvent<float> onNormalizedValueChanged;
    [Space]
    public RectTransform graphLowLineRectTransform;
    public RectTransform graphHighLineRectTransform;
    public RectTransform currentVerticalLineRectTransform;
    public RectTransform currentHorizontalLineRectTransform;
    public RectTransform currentValueTextRectTransform;


    void Start()
    {
        SetActiveAll(graphOnObjects, false);
        SetActiveAll(graphOffObjects, true);
        UpdateHeartRate(0f);
        UpdateGraph();
    }

    public void UpdateHeartRate(float heartrateHz)
    {
        currentHeartRate = heartrateHz;

        // Update visual state
        if (currentHeartRate > 0)
        {
            SetActiveAll(graphOnObjects, true);
            SetActiveAll(graphOffObjects, false);
        }
        else
        {
            SetActiveAll(graphOnObjects, false);
            SetActiveAll(graphOffObjects, true);
        }

        // Update animator
        if (heartrateAnimator != null && heartrateAnimator.isActiveAndEnabled)
        {
            heartrateAnimator.SetFloat(heartrateAnimatorParam, currentHeartRate);
        }

        UpdateNormalized();
        UpdateGraph();
    }
    public void SetHeartrateMin(float min)
    {
        minValue = min;
        UpdateNormalized();
        UpdateGraph();
    }
    public void SetHeartrateMax(float max)
    {
        maxValue = max;
        UpdateNormalized();
        UpdateGraph();
    }

    // Compute normalized value and invoke event
    void UpdateNormalized()
    {
        float normalized = 0f;
        if (maxValue != minValue)
        {
            if (currentHeartRate <= minValue)
            {
                normalized = 0f;
            }
            else if (currentHeartRate >= maxValue)
            {
                normalized = 1f;
            }
            else
            {
                normalized = (currentHeartRate - minValue) / (maxValue - minValue);
            }
        }
        normalizedValue = normalized;
        onNormalizedValueChanged.Invoke(normalized);
    }

    // Update graph visual based on min/max values
    void UpdateGraph()
    {
        if (maxValue == 0f)
        {
            // Avoid division by zero
            maxValue += float.Epsilon;
        }

        if (graphLowLineRectTransform != null)
        {
            float anchorMaxX = Mathf.Clamp01((minValue / maxValue) * highLineStartOffset);
            graphLowLineRectTransform.anchorMin = new Vector2(0f, 0f);
            graphLowLineRectTransform.anchorMax = new Vector2(anchorMaxX, 0f);
        }
        if (graphHighLineRectTransform != null)
        {
            float anchorMinX = highLineStartOffset;
            graphHighLineRectTransform.anchorMin = new Vector2(anchorMinX, 1f);
            graphHighLineRectTransform.anchorMax = new Vector2(1f, 1f);
        }

        // Update current heart rate lines
        float anchorPosX = Mathf.Clamp01((currentHeartRate / maxValue) * highLineStartOffset);
        float anchorPosY = normalizedValue;
        Vector2 anchorMin;
        if (currentVerticalLineRectTransform != null)
        {
            anchorMin = currentVerticalLineRectTransform.anchorMin;
            anchorMin.x = anchorPosX;
            currentVerticalLineRectTransform.anchorMin = anchorMin;
            
            currentVerticalLineRectTransform.anchorMax = new Vector2(anchorPosX, anchorPosY);
        }
        if (currentHorizontalLineRectTransform != null)
        {
            anchorMin = currentHorizontalLineRectTransform.anchorMin;
            anchorMin.y = anchorPosY;
            currentHorizontalLineRectTransform.anchorMin = anchorMin;
            currentHorizontalLineRectTransform.anchorMax = new Vector2(anchorPosX, anchorPosY);
        }

        if (currentValueTextRectTransform != null)
        {
            Vector2 pivot = currentValueTextRectTransform.pivot;
            if (normalizedValue > 0.9f)
            {
                pivot.y = (normalizedValue - 0.9f) * 5f + 0.5f;
            }
            else if (normalizedValue < 0.1f)
            {
                pivot.y = (normalizedValue - 0.1f) * 5f + 0.5f;
            }
            else
            {
                pivot.y = 0.5f;
            }
            currentValueTextRectTransform.pivot = pivot;
        }
    }

    void SetActiveAll(GameObject[] gameObjects, bool active)
    {
        foreach (GameObject obj in gameObjects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

}
