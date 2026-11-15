using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connect two points on a Canvas (RectTransform) using an "Image".
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform), typeof(Image))]
public class UILineBetweenPoints : CanvasGeometryUpdater
{
    [Header("Points to connect")]
    public RectTransform referenceRootRT;  // Reference root RectTransform for coordinate system
    public RectTransform pointA;   // First point (start)
    public RectTransform pointB;   // Second point (end)

    [Header("Line thickness (height)")]
    public float lineThickness = 2f;

    private RectTransform lineRect;
    private RectTransform previousRoot;
    private RectTransform previousA;
    private RectTransform previousB;
    private Matrix4x4 previousRootMatrix;
    private Vector3 previousPositionA;
    private Vector3 previousPositionB;
    private float previousThickness;
    private Vector3 appliedPosition;
    private Vector2 appliedSize;
    private Quaternion appliedRotation;
    private bool hasSnapshot;

    protected override void UpdateGeometryIfChanged(bool forceRefresh)
    {
        if (pointA == null || pointB == null || referenceRootRT == null)
            return;
        if (lineRect == null) lineRect = GetComponent<RectTransform>();

        Vector3 positionA = pointA.position;
        Vector3 positionB = pointB.position;
        Matrix4x4 rootMatrix = referenceRootRT.worldToLocalMatrix;
        if (!forceRefresh && hasSnapshot && previousRoot == referenceRootRT &&
            previousA == pointA && previousB == pointB && previousRootMatrix == rootMatrix &&
            previousPositionA == positionA && previousPositionB == positionB &&
            previousThickness == lineThickness && lineRect.localPosition == appliedPosition &&
            lineRect.sizeDelta == appliedSize && lineRect.localRotation == appliedRotation)
            return;

        Vector2 screenA = RectTransformUtility.WorldToScreenPoint(null, positionA);
        Vector2 screenB = RectTransformUtility.WorldToScreenPoint(null, positionB);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRootRT, screenA, null, out Vector2 canvasPosA);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRootRT, screenB, null, out Vector2 canvasPosB);

        Vector2 dir = canvasPosB - canvasPosA;
        float length = dir.magnitude;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        appliedPosition = canvasPosA;
        appliedSize = new Vector2(length, lineThickness);
        appliedRotation = Quaternion.Euler(0f, 0f, angle);
        Vector2 pivot = new Vector2(0f, 0.5f);
        if (lineRect.pivot != pivot) lineRect.pivot = pivot;
        if (lineRect.localPosition != appliedPosition) lineRect.localPosition = appliedPosition;
        if (lineRect.sizeDelta != appliedSize) lineRect.sizeDelta = appliedSize;
        if (lineRect.localRotation != appliedRotation) lineRect.localRotation = appliedRotation;

        previousRoot = referenceRootRT;
        previousA = pointA;
        previousB = pointB;
        previousRootMatrix = rootMatrix;
        previousPositionA = positionA;
        previousPositionB = positionB;
        previousThickness = lineThickness;
        hasSnapshot = true;
    }
}
