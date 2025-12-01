using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sync position to reference RectTransform.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIPositionSyncer : MonoBehaviour
{
    [Header("Points to connect")]
    public RectTransform referenceRootRT;  // Reference root RectTransform for coordinate system
    public RectTransform syncTarget;   // Target to sync position with

    void OnValidate()
    {
        Awake();
    }

    void Awake()
    {
        if (referenceRootRT == null)
        {
            Debug.LogError("Root canvas not found.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        UpdateLine();
    }

    void UpdateLine()
    {
        if (syncTarget == null || referenceRootRT == null)
            return;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, syncTarget.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRootRT, screenPos, null, out Vector2 canvasPos);

        transform.localPosition = canvasPos;
    }
}
