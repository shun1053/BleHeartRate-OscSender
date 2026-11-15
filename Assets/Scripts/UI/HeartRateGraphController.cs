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
    public UnityEvent<float> onNormalizedValueChanged = new UnityEvent<float>();
    [Space]
    public RectTransform graphLowLineRectTransform;
    public RectTransform graphHighLineRectTransform;
    public RectTransform currentVerticalLineRectTransform;
    public RectTransform currentHorizontalLineRectTransform;
    public RectTransform currentValueTextRectTransform;


    void OnEnable()
    {
        UpdateAnimationState();
    }

    void OnDisable()
    {
        if (heartrateAnimator != null) heartrateAnimator.enabled = false;
    }

    void Start()
    {
        UpdateHeartRate(0f);
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

        UpdateAnimationState();
        UpdateNormalized();
        UpdateGraph();
    }

    private void UpdateAnimationState()
    {
        if (heartrateAnimator == null) return;
        // Keep the heartbeat animation running when another application has focus.
        bool shouldAnimate = isActiveAndEnabled && currentHeartRate > 0f;
        if (heartrateAnimator.enabled != shouldAnimate) heartrateAnimator.enabled = shouldAnimate;
        if (shouldAnimate && heartrateAnimator.gameObject.activeInHierarchy)
            heartrateAnimator.SetFloat(heartrateAnimatorParam, currentHeartRate);
    }
    public void SetHeartrateMinWithSave(float min)
    {
        AppSettingsManager.SaveMinValue(min);
        SetHeartrateMin(min);
    }
    public void SetHeartrateMin(float min)
    {
        minValue = min;
        UpdateNormalized();
        UpdateGraph();
    }
    public void SetHeartrateMaxWithSave(float max)
    {
        AppSettingsManager.SaveMaxValue(max);
        SetHeartrateMax(max);
    }
    public void SetHeartrateMax(float max)
    {
        maxValue = max;
        UpdateNormalized();
        UpdateGraph();
    }
    public void SaveQuarterHRSendTarget(string target)
    {
        AppSettingsManager.SaveQuarterHRSendTarget(target);
    }
    public void SaveNormalizedValueSendTarget(string target)
    {
        AppSettingsManager.SaveNormalizedValueSendTarget(target);
    }
    public void SaveSendTargetIP(string ip)
    {
        AppSettingsManager.SaveSendTargetIP(ip);
    }
    public void SaveSendTargetPort(int port)
    {
        AppSettingsManager.SaveSendTargetPort(port);
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
        // Do not mutate the normalization settings merely to draw a zero-range graph.
        float graphMaximum = maxValue == 0f ? float.Epsilon : maxValue;

        if (graphLowLineRectTransform != null)
        {
            float anchorMaxX = Mathf.Clamp01((minValue / graphMaximum) * highLineStartOffset);
            SetAnchors(graphLowLineRectTransform, Vector2.zero, new Vector2(anchorMaxX, 0f));
        }
        if (graphHighLineRectTransform != null)
        {
            float anchorMinX = highLineStartOffset;
            SetAnchors(graphHighLineRectTransform, new Vector2(anchorMinX, 1f), Vector2.one);
        }

        // Update current heart rate lines
        float anchorPosX = Mathf.Clamp01((currentHeartRate / graphMaximum) * highLineStartOffset);
        float anchorPosY = normalizedValue;
        Vector2 anchorMin;
        if (currentVerticalLineRectTransform != null)
        {
            anchorMin = currentVerticalLineRectTransform.anchorMin;
            anchorMin.x = anchorPosX;
            SetAnchors(currentVerticalLineRectTransform, anchorMin, new Vector2(anchorPosX, anchorPosY));
        }
        if (currentHorizontalLineRectTransform != null)
        {
            anchorMin = currentHorizontalLineRectTransform.anchorMin;
            anchorMin.y = anchorPosY;
            SetAnchors(currentHorizontalLineRectTransform, anchorMin, new Vector2(anchorPosX, anchorPosY));
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
            if (currentValueTextRectTransform.pivot != pivot) currentValueTextRectTransform.pivot = pivot;
        }
    }

    private static void SetAnchors(RectTransform target, Vector2 minimum, Vector2 maximum)
    {
        if (target.anchorMin != minimum) target.anchorMin = minimum;
        if (target.anchorMax != maximum) target.anchorMax = maximum;
    }

    void SetActiveAll(GameObject[] gameObjects, bool active)
    {
        if (gameObjects == null) return;
        foreach (GameObject obj in gameObjects)
        {
            if (obj != null && obj.activeSelf != active)
            {
                obj.SetActive(active);
            }
        }
    }

}
