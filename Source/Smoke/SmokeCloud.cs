using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(VisionBlocker))]
public class SmokeCloud : MonoBehaviour
{
    private static readonly List<SmokeCloud> ActiveSmokeClouds = new();

    [Header("References")]
    [SerializeField] private PolygonCollider2D smokeCollider;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer[] visualRenderers;

    [Header("Visual Settings")]
    [SerializeField] private float visualDiameterAtScaleOne = 1f;
    [SerializeField] private float shapeRefreshInterval = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool drawColliderGizmos = true;

    private ThrowableData smokeData;
    private float currentRadius;
    private float currentAlpha;
    private bool initialized;

    private VisionBlocker visionBlocker;

    public static IReadOnlyList<SmokeCloud> ActiveClouds => ActiveSmokeClouds;

    private void Awake()
    {
        if (smokeCollider == null)
            smokeCollider = GetComponent<PolygonCollider2D>();

        if (visualRenderers == null || visualRenderers.Length == 0)
            visualRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        visionBlocker = GetComponent<VisionBlocker>();

        smokeCollider.isTrigger = true;

        // visualRoot를 비워둘 수는 있지만,
        // 이 경우 루트 transform 자체를 스케일하지 않도록 ApplyVisuals에서 방어 처리함
    }

    private void OnEnable()
    {
        if (!ActiveSmokeClouds.Contains(this))
            ActiveSmokeClouds.Add(this);
    }

    private void OnDisable()
    {
        ActiveSmokeClouds.Remove(this);
    }

    public void Initialize(ThrowableData data)
    {
        smokeData = data;
        initialized = true;

        if (visionBlocker == null)
            visionBlocker = GetComponent<VisionBlocker>();

        // 루트는 항상 스케일 1 유지
        transform.localScale = Vector3.one;

        StartCoroutine(RunSmokeRoutine());
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        if (smokeCollider == null || !smokeCollider.enabled)
            return false;

        return smokeCollider.OverlapPoint(worldPoint);
    }

    private IEnumerator RunSmokeRoutine()
    {
        if (!initialized || smokeData == null)
            yield break;

        float nextShapeRefreshTime = 0f;

        currentRadius = 0.01f;
        currentAlpha = smokeData.smokeMaxAlpha;
        RebuildSmokeShape(currentRadius);
        ApplyVisuals(currentRadius, currentAlpha);

        // 1. 확장
        float expandElapsed = 0f;
        while (expandElapsed < smokeData.smokeExpandTime)
        {
            expandElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(expandElapsed / smokeData.smokeExpandTime);

            currentRadius = Mathf.Lerp(0.01f, smokeData.smokeMaxRadius, t);
            currentAlpha = smokeData.smokeMaxAlpha;

            if (Time.time >= nextShapeRefreshTime || t >= 1f)
            {
                RebuildSmokeShape(currentRadius);
                nextShapeRefreshTime = Time.time + shapeRefreshInterval;
            }

            ApplyVisuals(currentRadius, currentAlpha);
            yield return null;
        }

        currentRadius = smokeData.smokeMaxRadius;
        currentAlpha = smokeData.smokeMaxAlpha;
        RebuildSmokeShape(currentRadius);
        ApplyVisuals(currentRadius, currentAlpha);

        // 2. 유지
        if (smokeData.smokeDuration > 0f)
            yield return new WaitForSeconds(smokeData.smokeDuration);

        // 3. 페이드 아웃
        float fadeElapsed = 0f;
        while (fadeElapsed < smokeData.smokeFadeTime)
        {
            fadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(fadeElapsed / smokeData.smokeFadeTime);

            currentAlpha = Mathf.Lerp(smokeData.smokeMaxAlpha, 0f, t);
            ApplyVisuals(currentRadius, currentAlpha);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void RebuildSmokeShape(float radius)
    {
        if (smokeCollider == null || smokeData == null)
            return;

        int sampleCount = Mathf.Max(8, smokeData.smokeBoundarySamples);
        Vector2[] points = new Vector2[sampleCount];

        float angleStep = 360f / sampleCount;
        Vector2 origin = transform.position;

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float finalDistance = radius;

            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                dir,
                radius,
                smokeData.smokeObstacleMask
            );

            if (hit.collider != null)
            {
                finalDistance = Mathf.Max(0f, hit.distance - smokeData.smokeWallPadding);
            }

            points[i] = dir * finalDistance;
        }

        smokeCollider.pathCount = 1;
        smokeCollider.SetPath(0, points);
    }

    private void ApplyVisuals(float radius, float alpha)
    {
        // 루트 transform에 collider가 있으므로 루트는 스케일하지 않음
        if (visualRoot != null && visualRoot != transform && visualDiameterAtScaleOne > 0.0001f)
        {
            float diameter = radius * 2f;
            float scale = diameter / visualDiameterAtScaleOne;
            visualRoot.localScale = new Vector3(scale, scale, 1f);
        }

        if (visualRenderers != null)
        {
            foreach (SpriteRenderer sr in visualRenderers)
            {
                if (sr == null)
                    continue;

                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawColliderGizmos || smokeCollider == null)
            return;

        Gizmos.color = new Color(0.7f, 0.9f, 1f, 0.9f);

        int pathCount = smokeCollider.pathCount;
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            Vector2[] points = smokeCollider.GetPath(pathIndex);
            if (points == null || points.Length < 2)
                continue;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 a = transform.TransformPoint(points[i]);
                Vector3 b = transform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(a, b);
            }
        }
    }
#endif
}*/