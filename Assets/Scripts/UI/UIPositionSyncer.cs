using UnityEngine;

/// <summary>
/// Sync position to reference RectTransform.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIPositionSyncer : CanvasGeometryUpdater
{
    [Header("Points to connect")]
    public RectTransform referenceRootRT;  // Reference root RectTransform for coordinate system
    public RectTransform syncTarget;   // Target to sync position with

    private RectTransform previousRoot;
    private RectTransform previousTarget;
    private Matrix4x4 previousRootMatrix;
    private Vector3 previousTargetPosition;
    private Vector3 appliedPosition;
    private bool hasSnapshot;

    protected override void UpdateGeometryIfChanged(bool forceRefresh)
    {
        if (syncTarget == null || referenceRootRT == null)
            return;

        Vector3 targetPosition = syncTarget.position;
        Matrix4x4 rootMatrix = referenceRootRT.worldToLocalMatrix;
        if (!forceRefresh && hasSnapshot && previousRoot == referenceRootRT &&
            previousTarget == syncTarget && previousRootMatrix == rootMatrix &&
            previousTargetPosition == targetPosition && transform.localPosition == appliedPosition)
            return;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRootRT, screenPos, null, out Vector2 canvasPos);

        appliedPosition = canvasPos;
        if (transform.localPosition != appliedPosition)
            transform.localPosition = appliedPosition;

        previousRoot = referenceRootRT;
        previousTarget = syncTarget;
        previousRootMatrix = rootMatrix;
        previousTargetPosition = targetPosition;
        hasSnapshot = true;
    }
}
