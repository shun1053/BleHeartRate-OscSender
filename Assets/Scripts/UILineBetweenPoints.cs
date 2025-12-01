using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connect two points on a Canvas (RectTransform) using an "Image".
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform), typeof(Image))]
public class UILineBetweenPoints : MonoBehaviour
{
    [Header("Points to connect")]
    public RectTransform referenceRootRT;  // Reference root RectTransform for coordinate system
    public RectTransform pointA;   // First point (start)
    public RectTransform pointB;   // Second point (end)

    [Header("Line thickness (height)")]
    public float lineThickness = 2f;

    RectTransform lineRect;

    void OnValidate()
    {
        Awake();
    }

    void Awake()
    {
        lineRect = GetComponent<RectTransform>();
        if (lineRect == null)
        {
            Debug.LogError("UILineBetweenPoints requires a RectTransform component.");
            enabled = false;
            return;
        }
        if (referenceRootRT == null) 
        {
            Debug.LogError("Root canvas not found.");
            enabled = false;
            return;
        }
        // Set the pivot of this RectTransform to the left edge (0, 0.5)
        lineRect.pivot = new Vector2(0f, 0.5f);
    }

    void Update()
    {
        UpdateLine();
    }

    void UpdateLine()
    {
        if (pointA == null || pointB == null || lineRect == null || referenceRootRT == null)
            return;

        // Assume both points share the same parent (coordinate system)
        Vector2 screenA = RectTransformUtility.WorldToScreenPoint(null, pointA.position);
        Vector2 screenB = RectTransformUtility.WorldToScreenPoint(null, pointB.position);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRootRT, screenA, null, out Vector2 canvasPosA);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRootRT, screenB, null, out Vector2 canvasPosB);

        Vector2 dir = canvasPosB - canvasPosA;
        float length = dir.magnitude;

        lineRect.localPosition = canvasPosA;
        lineRect.sizeDelta = new Vector2(length, lineThickness);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
