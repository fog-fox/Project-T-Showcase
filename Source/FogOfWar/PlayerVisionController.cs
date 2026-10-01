using System.Collections.Generic;
using UnityEngine;

public class PlayerVisionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FogOfWarGrid fogGrid;
    [SerializeField] private FogOfWarRenderer fogRenderer;
    [SerializeField] private Transform viewOrigin;
    [SerializeField] private Transform facingReference;

    [Header("Vision Settings")]
    [SerializeField] private float updateInterval = 0.15f;
    [SerializeField] private float nearRadius = 2.5f;
    [SerializeField] private float viewDistance = 8f;
    [SerializeField, Range(1f, 180f)] private float viewAngle = 100f;

    [Header("Optimization")]
    [SerializeField] private float minMoveDeltaToRefresh = 0.2f;
    [SerializeField] private float minAngleDeltaToRefresh = 4f;

    [Header("LOS")]
    [SerializeField] private LayerMask visionBlockMask;
    [SerializeField] private float rayOriginOffset = 0.05f;
    [SerializeField] private bool skipLOSInsideNearRadius = false;

    [Header("Smoke")]
    [SerializeField] private int fallbackInsideSmokeRequiredPuffs = 2;

    private float updateTimer;
    private bool hasInitializedVision;

    private Vector3 lastOrigin;
    private Vector2 lastForward;
    private bool lastInsideSmoke;

    private readonly HashSet<Vector2Int> currentVisibleCells = new();

    public IReadOnlyCollection<Vector2Int> CurrentVisibleCells => currentVisibleCells;

    private void Awake()
    {
        if (viewOrigin == null)
            viewOrigin = transform;
    }

    private void Start()
    {
        ForceRefreshVision();
    }

    private void Update()
    {
        if (fogGrid == null)
            return;

        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval)
            return;

        updateTimer = 0f;

        Vector3 currentOrigin = viewOrigin.position;
        Vector2 currentForward = GetFacingDirection();
        bool isInsideSmoke = IsInsideSmoke(currentOrigin);

        bool needRefresh = !hasInitializedVision
            || Vector3.Distance(currentOrigin, lastOrigin) >= minMoveDeltaToRefresh
            || Vector2.Angle(currentForward, lastForward) >= minAngleDeltaToRefresh
            || isInsideSmoke != lastInsideSmoke;

        if (!needRefresh)
            return;

        RefreshVision(currentOrigin, currentForward, isInsideSmoke);
    }

    public void ForceRefreshVision()
    {
        if (fogGrid == null)
            return;

        Vector3 currentOrigin = viewOrigin.position;
        Vector2 currentForward = GetFacingDirection();
        bool isInsideSmoke = IsInsideSmoke(currentOrigin);

        RefreshVision(currentOrigin, currentForward, isInsideSmoke);
    }

    private void RefreshVision(Vector3 origin, Vector2 forward, bool isInsideSmoke)
    {
        currentVisibleCells.Clear();

        float maxCheckDistance = isInsideSmoke
            ? nearRadius
            : Mathf.Max(nearRadius, viewDistance);

        int radiusInCells = Mathf.CeilToInt(maxCheckDistance / fogGrid.CellSize);
        Vector2Int centerCell = fogGrid.WorldToCell(origin);

        for (int x = centerCell.x - radiusInCells; x <= centerCell.x + radiusInCells; x++)
        {
            for (int y = centerCell.y - radiusInCells; y <= centerCell.y + radiusInCells; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (!fogGrid.IsInBounds(cell))
                    continue;

                Vector3 cellWorld = fogGrid.CellToWorldCenter(cell);
                Vector2 toCell = cellWorld - origin;
                float distance = toCell.magnitude;

                if (distance > maxCheckDistance)
                    continue;

                if (!IsInVisionShape(toCell, distance, forward, isInsideSmoke))
                    continue;

                if (!HasLineOfSight(origin, cellWorld, distance, cell, isInsideSmoke))
                    continue;

                currentVisibleCells.Add(cell);
            }
        }

        VisibilityChangeSet changes = fogGrid.UpdateVisibleCells(currentVisibleCells);

        if (fogRenderer != null && changes.HasAnyChange)
        {
            fogRenderer.ApplyChanges(changes);
        }

        lastOrigin = origin;
        lastForward = forward;
        lastInsideSmoke = isInsideSmoke;
        hasInitializedVision = true;
    }

    private Vector2 GetFacingDirection()
    {
        if (facingReference != null)
        {
            Vector2 dir = facingReference.right;
            if (dir.sqrMagnitude > 0.0001f)
                return dir.normalized;
        }

        return Vector2.right;
    }

    private bool IsInsideSmoke(Vector3 worldPos)
    {
        IReadOnlyList<SmokePuff> activePuffs = SmokePuff.ActiveSmokePuffs;
        int overlapCount = 0;
        int requiredCount = fallbackInsideSmokeRequiredPuffs;

        for (int i = 0; i < activePuffs.Count; i++)
        {
            SmokePuff puff = activePuffs[i];

            if (puff == null)
                continue;

            if (puff.ContainsWorldPoint(worldPos))
            {
                overlapCount++;

                if (overlapCount >= requiredCount)
                    return true;
            }
        }

        return false;
    }

    private bool IsInVisionShape(Vector2 toCell, float distance, Vector2 forward, bool isInsideSmoke)
    {
        // 주변시야는 항상 유지
        if (distance <= nearRadius)
            return true;

        // 연막 내부에서는 전방시야 제거
        if (isInsideSmoke)
            return false;

        if (distance > viewDistance)
            return false;

        float angle = Vector2.Angle(forward, toCell.normalized);
        return angle <= viewAngle * 0.5f;
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 target, float distance, Vector2Int targetCell, bool isInsideSmoke)
    {
        Vector2 dir = (target - origin).normalized;
        Vector3 start = origin + (Vector3)(dir * rayOriginOffset);

        RaycastHit2D[] hits = Physics2D.RaycastAll(start, dir, distance, visionBlockMask);
        if (hits == null || hits.Length == 0)
            return true;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool isNearVision = distance <= nearRadius;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];

            if (hit.collider == null)
                continue;

            VisionBlocker blocker = hit.collider.GetComponent<VisionBlocker>();
            if (blocker == null)
                blocker = hit.collider.GetComponentInParent<VisionBlocker>();

            if (blocker == null)
                continue;

            // 주변시야는 연막에 절대 가려지지 않음
            if (isNearVision && blocker.BlockerType == VisionBlockerType.Smoke)
            {
                continue;
            }

            // 전방시야/원거리 시야는 연막에 막힘
            if (blocker.BlockerType == VisionBlockerType.Smoke)
            {
                return false;
            }

            if (blocker.BlockerType == VisionBlockerType.Wall)
            {
                Vector2Int hitCell = fogGrid.WorldToCell(hit.point);

                // 벽 셀 자체는 보이게 허용
                if (hitCell == targetCell)
                    return true;

                return false;
            }
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform originRef = viewOrigin != null ? viewOrigin : transform;
        Vector3 origin = originRef.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, nearRadius);

        Vector2 forward = facingReference != null ? (Vector2)facingReference.right : Vector2.right;

        Vector2 left = Quaternion.Euler(0f, 0f, viewAngle * 0.5f) * forward;
        Vector2 right = Quaternion.Euler(0f, 0f, -viewAngle * 0.5f) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + (Vector3)(left * viewDistance));
        Gizmos.DrawLine(origin, origin + (Vector3)(right * viewDistance));
        Gizmos.DrawLine(origin, origin + (Vector3)(forward * viewDistance));
    }
#endif
}